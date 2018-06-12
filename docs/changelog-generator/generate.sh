#!/usr/bin/env bash

GENERATED=../changelog
FILENAME="$GENERATED/$1.md"

if [ ! -d "$GENERATED" ]; then
  mkdir "$GENERATED"
fi

echo "---" > $FILENAME
echo "uid: changelog.$1" >> $FILENAME
echo "---" >> $FILENAME
echo "" >> $FILENAME
echo "# BenchmarkDotNet $1" >> $FILENAME
echo "" >> $FILENAME
echo "" >> $FILENAME

if [ -f header/$1.md ]; then
  cat header/$1.md >> $FILENAME
  echo "" >> $FILENAME
  echo "" >> $FILENAME
fi

if [ -f details/$1.md ]; then
  cat details/$1.md >> $FILENAME
  echo "" >> $FILENAME
  echo "" >> $FILENAME
fi

if [ -f footer/$1.md ]; then
  echo "## Additional details" >> $FILENAME
  echo "" >> $FILENAME
  echo "" >> $FILENAME
  cat footer/$1.md >> $FILENAME
fi
