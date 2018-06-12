#!/usr/bin/env bash

start=`date +%s`
echo "$1: download started"

cd ChangeLogBuilder
dotnet run -- "$@"
cd ..

end=`date +%s`
echo "$1: download fininshed ($((end-start)) seconds)"

DETAILS=details

if [ ! -d "$DETAILS" ]; then
  mkdir "$DETAILS"
fi

cp ChangeLogBuilder/$1.md $DETAILS/$1.md