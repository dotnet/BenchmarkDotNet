BenchmarkDotNetVersion <- "$BenchmarkDotNetVersion$ "
dir.create(Sys.getenv("R_LIBS_USER"), recursive = TRUE, showWarnings = FALSE)
list.of.packages <- c("ggplot2", "dplyr", "gdata", "tidyr", "grid", "gridExtra")
new.packages <- list.of.packages[!(list.of.packages %in% installed.packages()[,"Package"])]
if(length(new.packages)) install.packages(new.packages, lib = Sys.getenv("R_LIBS_USER"), repos = "http://cran.rstudio.com/")
library(ggplot2)
library(dplyr)
library(gdata)
library(tidyr)
library(grid)
library(gridExtra)

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
BenchmarkDotNetVersionGrob <- textGrob(BenchmarkDotNetVersion, gp = gpar(fontface=3, fontsize=10), hjust=1, x=1)
nicePlot <- function(p) grid.arrange(p, bottom = BenchmarkDotNetVersionGrob)
printNice <- function(p) print(nicePlot(p))
ggsaveNice <- function(fileName, p, ...) ggsave(fileName, plot = nicePlot(p), ...)

args <- commandArgs(trailingOnly = TRUE)
files <- if (length(args) > 0) args else list.files()[list.files() %>% ends_with("-measurements.csv")]
for (file in files) {
  title <- gsub("-measurements.csv", "", basename(file))
  measurements <- read.csv(file, sep = ";")
  
  result <- measurements %>% filter(MeasurementIterationMode == "Result")
  if (nrow(result[is.na(result$Job),]) > 0)
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
  
  printNice(benchmarkBoxplot)
  printNice(benchmarkBarplot)
  ggsaveNice(gsub("-measurements.csv", "-boxplot.png", file), benchmarkBoxplot)
  ggsaveNice(gsub("-measurements.csv", "-barplot.png", file), benchmarkBarplot)
  
  for (target in unique(result$TargetMethod)) {
    df <- result %>% filter(TargetMethod == target)
    df$Launch <- factor(df$MeasurementLaunchIndex)
    densityPlot <- ggplot(df, aes(x=MeasurementValue, fill=Job)) + 
      ggtitle(paste(title, "/", target)) +
      xlab(paste("Time,", timeUnit)) +
      geom_density(alpha=.5)
    printNice(densityPlot)
    ggsaveNice(gsub("-measurements.csv", paste0("-", target, "-density.png"), file), densityPlot)
    
    for (job in unique(df$Job)) {
      jobDf <- df %>% filter(Job == job)
      timelinePlot <- ggplot(jobDf, aes(x = MeasurementIterationIndex, y=MeasurementValue, group=Launch, color=Launch)) + 
        ggtitle(paste(title, "/", target, "/", job)) +
        xlab("IterationIndex") +
        ylab(paste("Time,", timeUnit)) +
        geom_line() +
        geom_point()
      printNice(timelinePlot)
      ggsaveNice(gsub("-measurements.csv", paste0("-", target, "-", job, "-timeline.png"), file), timelinePlot)
      timelinePlotSmooth <- timelinePlot + geom_smooth()
      printNice(timelinePlotSmooth)
      ggsaveNice(gsub("-measurements.csv", paste0("-", target, "-", job, "-timelineSmooth.png"), file), timelinePlotSmooth)
    }
    
    timelinePlot <- ggplot(df, aes(x = MeasurementIterationIndex, y=MeasurementValue, group=Launch, color=Launch)) + 
      ggtitle(paste(title, "/", target)) +
      xlab("IterationIndex") +
      ylab(paste("Time,", timeUnit)) +
      geom_line() +
      geom_point() +
      facet_wrap(~Job)
    printNice(timelinePlot)
    ggsaveNice(gsub("-measurements.csv", paste0("-", target, "-facetTimeline.png"), file), timelinePlot)
    timelinePlotSmooth <- timelinePlot + geom_smooth()
    printNice(timelinePlotSmooth)
    ggsaveNice(gsub("-measurements.csv", paste0("-", target, "-facetTimelineSmooth.png"), file), timelinePlotSmooth)
  }
}