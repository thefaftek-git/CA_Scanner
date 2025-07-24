#!/bin/bash

ARCH_DIR="arch_test4"
echo -n "Enter GPG passphrase: "
read -s PASSPHRASE
echo

for file in "$ARCH_DIR"/*.tar.gz.gpg; do
    [ -e "$file" ] || continue
    base=$(basename "$file" .tar.gz.gpg)
    outdir="$ARCH_DIR/$base"
    mkdir -p "$outdir"
    # Decrypt to .tar.gz
    gpg --batch --yes --passphrase "$PASSPHRASE" -o "$ARCH_DIR/$base.tar.gz" -d "$file"
    # Extract .tar.gz
    tar -xzf "$ARCH_DIR/$base.tar.gz" -C "$outdir"
    # Optionally remove the decrypted .tar.gz
    rm "$ARCH_DIR/$base.tar.gz"
    echo "Decompressed and extracted: $file -> $outdir"
done