#!/usr/bin/env bash

cp README.md addon/DOCS.md
cp README.md addon/.

VERSION=$(cat addon/config.yaml| grep version | grep -P -o "[\d\.]*")

echo git tag -a $VERSION -m $VERSION
