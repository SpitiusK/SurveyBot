#!/bin/bash
# =============================================================================
# SurveyBot VPS Setup Script
# =============================================================================
# This script automates the initial setup of a Ubuntu 24.04 VPS for SurveyBot.
#
# Usage:
#   curl -fsSL https://your-repo/setup-vps.sh | bash
#   # OR
#   chmod +x setup-vps.sh && ./setup-vps.sh
#
# What this script does:
#   1. Updates system packages
#   2. Installs Docker and Docker Compose
#   3. Installs Nginx
#   4. Configures firewall (UFW)
#   5. Installs Certbot for SSL certificates
#
# Requirements:
#   - Ubuntu 24.04 LTS
#   - Root or sudo access
#   - Internet connection
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if running as root or with sudo
check_sudo() {
    if [[ $EUID -ne 0 ]]; then
        if ! command -v sudo &> /dev/null; then
            log_error "This script must be run as root or with sudo"
            exit 1
        fi
        SUDO="sudo"
    else
        SUDO=""
    fi
}

# Check Ubuntu version
check_ubuntu() {
    if [[ -f /etc/os-release ]]; then
        . /etc/os-release
        if [[ "$ID" != "ubuntu" ]]; then
            log_warning "This script is designed for Ubuntu. Your OS: $ID"
            read -p "Continue anyway? (y/N) " -n 1 -r
            echo
            if [[ ! $REPLY =~ ^[Yy]$ ]]; then
                exit 1
            fi
        fi
    fi
}

# Update system
update_system() {
    log_info "Updating system packages..."
    $SUDO apt update
    $SUDO apt upgrade -y
    log_success "System updated"
}

# Install Docker
install_docker() {
    if command -v docker &> /dev/null; then
        log_info "Docker is already installed: $(docker --version)"
        return
    fi

    log_info "Installing Docker..."

    # Remove old versions if any
    $SUDO apt remove -y docker docker-engine docker.io containerd runc 2>/dev/null || true

    # Install dependencies
    $SUDO apt install -y ca-certificates curl gnupg lsb-release

    # Add Docker's official GPG key
    $SUDO install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | $SUDO gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    $SUDO chmod a+r /etc/apt/keyrings/docker.gpg

    # Set up repository
    echo \
      "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
      $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
      $SUDO tee /etc/apt/sources.list.d/docker.list > /dev/null

    # Install Docker
    $SUDO apt update
    $SUDO apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Add current user to docker group
    if [[ -n "$SUDO_USER" ]]; then
        $SUDO usermod -aG docker "$SUDO_USER"
        log_info "Added $SUDO_USER to docker group"
    elif [[ -n "$USER" && "$USER" != "root" ]]; then
        $SUDO usermod -aG docker "$USER"
        log_info "Added $USER to docker group"
    fi

    # Start and enable Docker
    $SUDO systemctl start docker
    $SUDO systemctl enable docker

    log_success "Docker installed: $(docker --version)"
}

# Install Nginx
install_nginx() {
    if command -v nginx &> /dev/null; then
        log_info "Nginx is already installed: $(nginx -v 2>&1)"
        return
    fi

    log_info "Installing Nginx..."
    $SUDO apt install -y nginx
    $SUDO systemctl enable nginx
    $SUDO systemctl start nginx
    log_success "Nginx installed"
}

# Configure firewall
configure_firewall() {
    log_info "Configuring firewall (UFW)..."

    $SUDO apt install -y ufw

    # Allow SSH (important - do this first!)
    $SUDO ufw allow OpenSSH

    # Allow Nginx (HTTP and HTTPS)
    $SUDO ufw allow 'Nginx Full'

    # Enable firewall
    echo "y" | $SUDO ufw enable

    log_success "Firewall configured"
    $SUDO ufw status
}

# Install Certbot for SSL
install_certbot() {
    if command -v certbot &> /dev/null; then
        log_info "Certbot is already installed"
        return
    fi

    log_info "Installing Certbot for SSL certificates..."
    $SUDO apt install -y certbot python3-certbot-nginx
    log_success "Certbot installed"
}

# Install additional useful tools
install_tools() {
    log_info "Installing additional tools..."
    $SUDO apt install -y \
        git \
        htop \
        nano \
        curl \
        wget \
        unzip
    log_success "Additional tools installed"
}

# Create project directory
create_project_dir() {
    PROJECT_DIR="/home/${SUDO_USER:-$USER}/surveybot"

    if [[ -d "$PROJECT_DIR" ]]; then
        log_info "Project directory already exists: $PROJECT_DIR"
        return
    fi

    log_info "Creating project directory: $PROJECT_DIR"
    mkdir -p "$PROJECT_DIR"

    if [[ -n "$SUDO_USER" ]]; then
        chown -R "$SUDO_USER:$SUDO_USER" "$PROJECT_DIR"
    fi

    log_success "Project directory created: $PROJECT_DIR"
}

# Print summary and next steps
print_summary() {
    echo
    echo "============================================================================="
    echo -e "${GREEN}VPS Setup Complete!${NC}"
    echo "============================================================================="
    echo
    echo "Installed components:"
    echo "  - Docker: $(docker --version 2>/dev/null || echo 'N/A')"
    echo "  - Docker Compose: $(docker compose version 2>/dev/null || echo 'N/A')"
    echo "  - Nginx: $(nginx -v 2>&1 | cut -d'/' -f2 || echo 'N/A')"
    echo "  - Certbot: $(certbot --version 2>/dev/null | cut -d' ' -f2 || echo 'N/A')"
    echo
    echo "Firewall status:"
    $SUDO ufw status | head -10
    echo
    echo "============================================================================="
    echo "NEXT STEPS:"
    echo "============================================================================="
    echo
    echo "1. LOG OUT AND LOG BACK IN (for Docker group changes to take effect)"
    echo "   exit"
    echo "   ssh ${SUDO_USER:-$USER}@your-server-ip"
    echo
    echo "2. Get a domain (free with DuckDNS):"
    echo "   - Go to https://www.duckdns.org"
    echo "   - Login and create a subdomain"
    echo "   - Point it to your VPS IP: $(curl -s ifconfig.me 2>/dev/null || echo 'YOUR_IP')"
    echo
    echo "3. Clone your project:"
    echo "   cd ~/surveybot"
    echo "   git clone YOUR_REPOSITORY_URL ."
    echo
    echo "4. Configure environment:"
    echo "   cp .env.example .env.production"
    echo "   nano .env.production  # Edit with your settings"
    echo
    echo "5. Deploy with Docker Compose:"
    echo "   docker compose -f docker-compose.production.yml up -d"
    echo
    echo "6. Configure Nginx:"
    echo "   sudo cp deployment/nginx/surveybot.conf /etc/nginx/sites-available/surveybot"
    echo "   sudo nano /etc/nginx/sites-available/surveybot  # Set your domain"
    echo "   sudo ln -s /etc/nginx/sites-available/surveybot /etc/nginx/sites-enabled/"
    echo "   sudo rm /etc/nginx/sites-enabled/default"
    echo "   sudo nginx -t && sudo systemctl reload nginx"
    echo
    echo "7. Get SSL certificate:"
    echo "   sudo certbot --nginx -d YOUR_DOMAIN"
    echo
    echo "============================================================================="
    echo "For detailed instructions, see: deployment/VPS_DEPLOYMENT_GUIDE.md"
    echo "============================================================================="
}

# Main execution
main() {
    echo "============================================================================="
    echo "SurveyBot VPS Setup Script"
    echo "============================================================================="
    echo

    check_sudo
    check_ubuntu

    log_info "Starting VPS setup..."
    echo

    update_system
    install_docker
    install_nginx
    configure_firewall
    install_certbot
    install_tools
    create_project_dir

    print_summary
}

# Run main function
main "$@"
