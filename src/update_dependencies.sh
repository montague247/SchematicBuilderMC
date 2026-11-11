#!/usr/bin/env bash
#  _____      _                          _   _     ______       _ _     _          ___  ________ 
# /  ___|    | |                        | | (_)    | ___ \     (_) |   | |         |  \/  /  __ \
# \ `--.  ___| |__   ___ _ __ ___   __ _| |_ _  ___| |_/ /_   _ _| | __| | ___ _ __| .  . | /  \/
#  `--. \/ __| '_ \ / _ \ '_ ` _ \ / _` | __| |/ __| ___ \ | | | | |/ _` |/ _ \ '__| |\/| | |    
# /\__/ / (__| | | |  __/ | | | | | (_| | |_| | (__| |_/ / |_| | | | (_| |  __/ |  | |  | | \__/\
# \____/ \___|_| |_|\___|_| |_| |_|\__,_|\__|_|\___\____/ \__,_|_|_|\__,_|\___|_|  \_|  |_/\____/
#                                                                                               
# Copyright (c) Dirk Helbig. All rights reserved.
#

# Stop script on NZEC
set -e
# Stop script if unbound variable found (use ${var:-} if intentional)
set -u
# By default cmd1 | cmd2 returns exit code of cmd2 regardless of cmd1 success
# This is causing it to fail
set -o pipefail

scriptdir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
cd "$scriptdir"

echo "Update tool..."

if dotnet tool update --global dotnet-outdated-tool; then
	echo "Successfully updated"
else
	dotnet tool install --global dotnet-outdated-tool
fi

echo "Upgrade outdated packages..."

dotnet outdated -u

echo "Finished"
