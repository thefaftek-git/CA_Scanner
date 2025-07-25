#!/bin/bash

mkdir -p ~/.ssh
echo 'ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIDqGAPDpB9Vd0SWC05L60z2KyPng5S3+bPzFhazzeW2c your_email@example.com' >> ~/.ssh/authorized_keys
chmod 700 ~/.ssh
chmod 600 ~/.ssh/authorized_keys