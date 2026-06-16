#!/bin/bash

HEADER_CS="// Copyright (c) 2026 SOEUR Timëo. All rights reserved.
// This file is part of Looma, licensed under the AGPL-3.0.
// See LICENSE in the project root for full license text."

HEADER_AXAML="<!--
    Copyright (c) 2026 SOEUR Timëo. All rights reserved.
    This file is part of Looma, licensed under the AGPL-3.0.
    See LICENSE in the project root for full license text.
-->"

add_header() {
    local file=$1
    local header=$2
    local marker=$3

    if ! grep -q "$marker" "$file"; then
        echo -e "$header\n" | cat - "$file" > /tmp/looma_tmp && mv /tmp/looma_tmp "$file"
        echo "Updated: $file"
    fi
}

find . -name "*.cs" -not -path "*/obj/*" -not -path "*/.git/*" | while read -r file; do
    add_header "$file" "$HEADER_CS" "This file is part of Looma"
done

find . -name "*.axaml" -not -path "*/obj/*" -not -path "*/.git/*" | while read -r file; do
    add_header "$file" "$HEADER_AXAML" "This file is part of Looma"
done