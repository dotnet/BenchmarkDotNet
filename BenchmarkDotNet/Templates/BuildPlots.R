dir.create(Sys.getenv("R_LIBS_USER"), recursive = TRUE, showWarnings = FALSE)
list.of.packages <- c("ggplot2", "dplyr", "gdata", "tidyr")
new.packages <- list.of.packages[!(list.of.packages %in% installed.packages()[,"Package"])]
if(length(new.packages)) install.packages(new.packages, lib = Sys.getenv("R_LIBS_USER"), repos = "http://cran.rstudio.com/")
library(ggplot2)
library(dplyr)
library(gdata)
library(tidyr)

ends_with <- function(vars, match, ignore.case = TRUE) {
  if (ignore.case) 
    match <- tolower(match)
  n <- nchar(match)
  
  if (ignore.case) 
    vars <- tolower(vars)
  length <- nchar(vars)
  
  substr(vars, pmax(1, length - n + 1), length) == match
}
std.error <- function(x) sqrt(var(x)/length(x))

args <- commandArgs(trailingOnly = TRUE)
files <- if (length(args) > 0) args else list.files()[list.files() %>% ends_with("-measurements.csv")]
for (file in files) {
  title <- gsub("-measurements.csv", "", basename(file))
  measurements <- read.csv(file, sep = ";")
  
  result <- measurements %>% filter(MeasurementIterationMode == "Result")
  result[is.na(result$Job),]$Job <- ""
  
  timeUnit <- "ns"
  if (min(result$MeasurementValue) > 1000) {
    result$MeasurementValue <- result$MeasurementValue / 1000
    timeUnit <- "us"
  }
  if (min(result$MeasurementValue) > 1000) {
    result$MeasurementValue <- result$MeasurementValue / 1000
    timeUnit <- "ms"
  }
  if (min(result$MeasurementValue) > 1000) {
    result$MeasurementValue <- result$MeasurementValue / 1000
    timeUnit <- "sec"
  }
  
  resultStats <- result %>% 
    group_by_(.dots = c("TargetMethod", "Job")) %>% 
    summarise(se = std.error(MeasurementValue), Value = mean(MeasurementValue))
  
  benchmarkBoxplot <- ggplot(result, aes(x=TargetMethod, y=MeasurementValue, fill=Job)) + 
    guides(fill=guide_legend(title="Job")) +
    xlab("Target") +
    ylab(paste("Time,", timeUnit)) +
    ggtitle(title) +
    geom_boxplot()
  benchmarkBarplot <- ggplot(resultStats, aes(x=TargetMethod, y=Value, fill=Job)) + 
    guides(fill=guide_legend(title="Job")) +
    xlab("Target") +
    ylab(paste("Time,", timeUnit)) + 
    ggtitle(title) +
    geom_bar(position=position_dodge(), stat="identity")
    #geom_errorbar(aes(ymin=Value-1.96*se, ymax=Value+1.96*se), width=.2, position=position_dodge(.9))
  
  print(benchmarkBoxplot)
  print(benchmarkBarplot)
  ggsave(gsub("-measurements.csv", "-boxplot.png", file), benchmarkBoxplot)
  ggsave(gsub("-measurements.csv", "-barplot.png", file), benchmarkBarplot)
  
  for (target in unique(result$TargetMethod)) {
    df <- (result %>% filter(TargetMethod == target))
    densityPlot <- ggplot(df, aes(x=MeasurementValue, fill=Job)) + 
      ggtitle(paste(title, "/", target)) +
      xlab(paste("Time,", timeUnit)) +
      geom_density(alpha=.5)
    print(densityPlot)
    ggsave(gsub("-measurements.csv", paste0("-", target, "-density.png"), file), densityPlot)
  }
}