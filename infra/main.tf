terraform {
  required_version = ">= 1.5"

  required_providers {
    scaleway = {
      source  = "scaleway/scaleway"
      version = "~> 2.40"
    }
    tls = {
      source  = "hashicorp/tls"
      version = "~> 4.0"
    }
  }
}

provider "scaleway" {
  region = var.region
  zone   = var.zone
}

resource "tls_private_key" "deploy" {
  algorithm = "ED25519"
}

resource "scaleway_iam_ssh_key" "deploy" {
  name       = "c4-deploy"
  public_key = tls_private_key.deploy.public_key_openssh
}

data "scaleway_marketplace_image" "ubuntu" {
  label         = "ubuntu_noble"
  instance_type = var.instance_type
}

resource "scaleway_instance_ip" "public" {}

resource "scaleway_instance_security_group" "c4" {
  name                    = "c4-sg"
  inbound_default_policy  = "drop"
  outbound_default_policy = "accept"

  inbound_rule {
    action   = "accept"
    port     = 22
    protocol = "TCP"
  }

  inbound_rule {
    action   = "accept"
    port     = 80
    protocol = "TCP"
  }

  inbound_rule {
    action   = "accept"
    port     = 443
    protocol = "TCP"
  }
}

resource "scaleway_instance_server" "c4" {
  name  = "c4-app"
  type  = var.instance_type
  image = data.scaleway_marketplace_image.ubuntu.id
  ip_id = scaleway_instance_ip.public.id

  security_group_id = scaleway_instance_security_group.c4.id

  root_volume {
    size_in_gb = 20
  }

  user_data = {
    cloud-init = file("${path.module}/cloud-init.yml")
  }

  tags = ["c4", "app"]

  depends_on = [scaleway_iam_ssh_key.deploy]
}

output "public_ip" {
  value = scaleway_instance_ip.public.address
}

output "server_id" {
  value = scaleway_instance_server.c4.id
}

output "ssh_private_key" {
  value     = tls_private_key.deploy.private_key_openssh
  sensitive = true
}
