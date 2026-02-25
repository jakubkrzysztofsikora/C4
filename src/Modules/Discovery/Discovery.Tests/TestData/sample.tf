resource "azurerm_resource_group" "rg" {
  name     = "platform-rg"
  location = "West Europe"
}

resource "azurerm_postgresql_flexible_server" "pg" {
  name                = "pg"
  resource_group_name = azurerm_resource_group.rg.name
}
