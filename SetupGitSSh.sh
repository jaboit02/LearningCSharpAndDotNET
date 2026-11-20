#!/bin/bash

# 1. Check if git and ssh are installed
for cmd in git ssh ssh-keygen ssh-keyscan; do
    if ! command -v "$cmd" &> /dev/null; then
        echo "Error: '$cmd' is not installed. Please install it first."
        exit 1
    fi
done

echo -e "\n=== Setting up GitHub SSH Keys ===\n"

# 2. Enter username and email
read -p "Enter your GitHub username: " username
read -p "Enter your GitHub email address for commits: " email

KEY_PATH="$HOME/.ssh/id_ed25519"

# 3. Generate SSH key
echo -e "\n--- Generating SSH Key ---"
mkdir -p "$HOME/.ssh"
chmod 700 "$HOME/.ssh"

if [ -f "$KEY_PATH" ]; then
    read -p "A key already exists at $KEY_PATH. Overwrite? (y/n): " overwrite
    if [[ "$overwrite" =~ ^[yY]$ ]]; then
        rm -f "$KEY_PATH" "${KEY_PATH}.pub"
        ssh-keygen -t ed25519 -C "$email" -f "$KEY_PATH" -N ""
    else
        echo "Using existing SSH key."
    fi
else
    ssh-keygen -t ed25519 -C "$email" -f "$KEY_PATH" -N ""
fi

# Start ssh-agent and add key
eval "$(ssh-agent -s)" > /dev/null
ssh-add "$KEY_PATH"


# Add GitHub to known_hosts to avoid interactive fingerprint prompt
# Example: „The authenticity of host 'github.com' can't be established. Are you sure you want to continue connecting (yes/no)?“
touch "$HOME/.ssh/known_hosts"
if ! grep -q "github.com" "$HOME/.ssh/known_hosts"; then
    ssh-keyscan -t ed25519 github.com >> "$HOME/.ssh/known_hosts" 2>/dev/null
fi

# 4. Instructions to add SSH key to GitHub
echo -e "\nPlease add this SSH Key to your GitHub Account (https://github.com):\n"
cat "${KEY_PATH}.pub"
echo ""
read -p "Press [Enter] once you have added the key to GitHub..."

# 5. Test SSH connection & abort if failed
echo -e "\n--- Testing SSH Connection ---"
SSH_OUTPUT=$(ssh -T git@github.com 2>&1)
echo "$SSH_OUTPUT"

if ! echo "$SSH_OUTPUT" | grep -q "successfully authenticated"; then
    echo -e "\nError: SSH authentication failed. Please check your key on GitHub and run the script again."
    exit 1
fi

# 6. Add global email and username to git config
echo -e "\n--- Configuring Git ---"
git config --global user.email "$email"
git config --global user.name "$username"
echo "Global git user.name and user.email set."

# 7. Clone Repository
echo -e "\n--- Cloning Repository ---"
read -p "Enter the SSH link to the repository: " repository
read -p "Name of the project folder (in $HOME): " projectName

if git clone "$repository" "$HOME/$projectName"; then
    echo -e "\nDone! Repository cloned to $HOME/$projectName"
else
    echo -e "\nError: Cloning failed. Check the repository URL or if the folder already exists."
    exit 1
fi


