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
files <- if (length(args) > 0) args else list.files()[list.files() %>% ends_with("-runs.csv")]
for (file in files) {
  title <- gsub("-runs.csv", "", file)
  runs <- read.csv(file, sep = ";")
  xColumns <- colnames(runs)[startsWith(colnames(runs), "Benchmark")]
  distinctValues <- sapply(xColumns, function(columnName) nrow(runs %>% select(get(columnName)) %>% unique))
  uniqueColumns <- xColumns[distinctValues > 1]
  sameColumns <- xColumns[distinctValues == 1]
  runs <- runs %>% select(-one_of(sameColumns))
  if (length(uniqueColumns) > 1) {
    runs <- runs %>% mutate(Env = unite_(runs, "Env", uniqueColumns[-1])$Env)
  } else {
    runs <- runs %>% mutate(Env = "")
  }
  runs <- runs %>% mutate(FullEnv = unite_(runs, "FullEnv", uniqueColumns)$FullEnv)
  
  targetRuns <- runs %>% filter(RunIterationMode == "Target")
  targetRuns$Value <- targetRuns$RunNanoseconds / targetRuns$RunOperations
  timeUnit <- "ns"
  if (min(targetRuns$Value) > 1000) {
    targetRuns$Value <- targetRuns$Value / 1000
    timeUnit <- "us"
  }
  if (min(targetRuns$Value) > 1000) {
    targetRuns$Value <- targetRuns$Value / 1000
    timeUnit <- "ms"
  }
  if (min(targetRuns$Value) > 1000) {
    targetRuns$Value <- targetRuns$Value / 1000
    timeUnit <- "sec"
  }
  targetRunsStats <- targetRuns %>% 
    group_by_(.dots = c(uniqueColumns, "Env", "FullEnv")) %>% 
    summarise(se = std.error(Value), Value = mean(Value))
  
  benchmarkBoxplot <- ggplot(targetRuns, aes(x=Env, y=Value, fill=get(uniqueColumns[1]))) + 
    guides(fill=guide_legend(title=uniqueColumns[1])) +
    xlab("Environment") +
    ylab(paste("Time,", timeUnit)) +
    ggtitle(title) +
    geom_boxplot()
  benchmarkBarplot <- ggplot(targetRunsStats, aes(x=Env, y=Value, fill=get(uniqueColumns[1]))) + 
    guides(fill=guide_legend(title=uniqueColumns[1])) +
    xlab("Environment") +
    ylab(paste("Time,", timeUnit)) + 
    ggtitle(title) +
    geom_bar(position=position_dodge(), stat="identity")
    #geom_errorbar(aes(ymin=Value-1.96*se, ymax=Value+1.96*se), width=.2, position=position_dodge(.9))
  
  print(benchmarkBoxplot)
  print(benchmarkBarplot)
  ggsave(gsub("-runs.csv", "-boxplot.png", file), benchmarkBoxplot)
  ggsave(gsub("-runs.csv", "-barplot.png", file), benchmarkBarplot)
}