#!/bin/bash
set -ev
dotnet restore
dotnet test
dotnet build -c Release -o app/build