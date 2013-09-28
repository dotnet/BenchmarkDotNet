library("Cairo")
trim <- function (x) gsub("^\\s+|\\s+$", "", x)
read.data <- function(filename) {
  t <- as.matrix(read.table(filename, sep=":"))
  for (i in 1:dim(t)[1]) {
    t[i, 1] = trim(t[i, 1])
    t[i, 2] = trim(substr(t[i, 2], 0, nchar(t[i, 2]) - 2))
  }
  t
}
ms.net3.5x86 <- read.data("ms-v3.5-x86.txt")
ms.net4.0x86 <- read.data("ms-v4.0-x86.txt")
ms.net4.5x86 <- read.data("ms-v4.5-x86.txt")
ms.net3.5x64 <- read.data("ms-v3.5-x64.txt")
ms.net4.0x64 <- read.data("ms-v4.0-x64.txt")
ms.net4.5x64 <- read.data("ms-v4.5-x64.txt")
mono3.5x86   <- read.data("mono-v3.5-x86.txt")
mono4.0x86   <- read.data("mono-v4.0-x86.txt")
mono4.5x86   <- read.data("mono-v4.5-x86.txt")
mono3.5x64   <- read.data("mono-v3.5-x64.txt")
mono4.0x64   <- read.data("mono-v4.0-x64.txt")
mono4.5x64   <- read.data("mono-v4.5-x64.txt")
rnames <- ms.net3.5x86[,1]

ms.net <- matrix(as.integer(c(
  ms.net3.5x86[,2], ms.net4.0x86[,2], ms.net4.5x86[,2],
  ms.net3.5x64[,2], ms.net4.0x64[,2], ms.net4.5x64[,2])), ncol=6)
mono   <- matrix(as.integer(c(
  mono3.5x86[,2], mono4.0x86[,2], mono4.5x86[,2],
  mono3.5x64[,2], mono4.0x64[,2], mono4.5x64[,2])), ncol=6)
factor <- 1 + 0.05 * length(rnames)
max.time <- max(max(ms.net), max(mono)) * factor
cnames <- c("3.5x86", "4.0x86", "4.5x86", "3.5x64", "4.0x64", "4.5x64")

v3.5 <- matrix(as.integer(c(ms.net3.5x86[,2], ms.net3.5x64[,2], mono3.5x86[,2], mono3.5x64[,2])), ncol=4)
v4.0 <- matrix(as.integer(c(ms.net4.0x86[,2], ms.net4.0x64[,2], mono4.0x86[,2], mono4.0x64[,2])), ncol=4)
v4.5 <- matrix(as.integer(c(ms.net4.5x86[,2], ms.net4.5x64[,2], mono4.5x86[,2], mono4.5x64[,2])), ncol=4)
cnamesv <- c("MS.NET-x86", "MS.NET-x64", "Mono-x86", "Mono-x64")

cols <- rainbow(length(rnames))
colnames(ms.net) <- cnames ; rownames(ms.net) <- rnames
colnames(mono) <- cnames   ; rownames(mono) <- rnames
colnames(v3.5) <- cnamesv  ; rownames(v3.5) <- rnames
colnames(v4.0) <- cnamesv  ; rownames(v4.0) <- rnames
colnames(v4.5) <- cnamesv  ; rownames(v4.5) <- rnames
CairoPNG("ms.net.png")
barplot(ms.net, main="MS.NET", xlab="Environment", ylab="ms", 
        col=cols, legend=rnames, beside=TRUE, ylim=c(0, max.time),
        args.legend = list(y = max.time * 1.05))
dev.off()
CairoPNG("mono.png")
barplot(mono, main="Mono", xlab="Environment", ylab="ms", 
        col = cols, legend=rnames, beside=TRUE, ylim=c(0, max.time),
        args.legend = list(y = max.time * 1.05))
dev.off()
CairoPNG("v3.5.png")
barplot(v3.5, main=".NET Framework 3.5", xlab="Environment", ylab="ms", 
        col = cols, legend=rnames, beside=TRUE, ylim=c(0, max.time),
        args.legend = list(y = max.time * 1.05))
dev.off()
CairoPNG("v4.0.png")
barplot(v4.0, main=".NET Framework 4.0", xlab="Environment", ylab="ms", 
        col = cols, legend=rnames, beside=TRUE, ylim=c(0, max.time),
        args.legend = list(y = max.time * 1.05))
dev.off()
CairoPNG("v4.5.png")
barplot(v4.5, main=".NET Framework 4.5", xlab="Environment", ylab="ms", 
        col = cols, legend=rnames, beside=TRUE, ylim=c(0, max.time),
        args.legend = list(y = max.time * 1.05))
dev.off()