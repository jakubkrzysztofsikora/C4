variable "region" {
  type    = string
  default = "fr-par"
}

variable "zone" {
  type    = string
  default = "fr-par-1"
}

variable "instance_type" {
  type    = string
  default = "DEV1-S"
}

variable "postgres_password" {
  type      = string
  sensitive = true
  default   = "c4prod2026!"
}
