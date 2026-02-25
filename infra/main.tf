terraform {
  required_version = ">= 1.5"

  required_providers {
    scaleway = {
      source  = "scaleway/scaleway"
      version = "~> 2.40"
    }
  }
}

provider "scaleway" {
  region = var.region
  zone   = var.zone
}

data "scaleway_marketplace_image" "ubuntu" {
  label          = "ubuntu_noble"
  instance_type  = var.instance_type
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
    cloud-init = templatefile("${path.module}/cloud-init.yml", {
      postgres_password = var.postgres_password
    })
  }

  tags = ["c4", "app"]
}

output "public_ip" {
  value = scaleway_instance_ip.public.address
}

output "app_url" {
  value = "http://${scaleway_instance_ip.public.address}"
}
