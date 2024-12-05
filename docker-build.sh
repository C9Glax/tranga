#!/usr/bin/env bash

set -x

docker run --rm --volume "$(pwd)":/workspace --workdir /workspace mcr.microsoft.com/dotnet/sdk:8.0 bash build.sh
