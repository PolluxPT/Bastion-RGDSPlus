#!/bin/sh
BASE=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd) || exit 1
export BASTION_REMAP_CONTROLS=1
exec "$BASE/Bastion.sh"
