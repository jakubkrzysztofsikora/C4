resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: 'frontend'
  location: 'westeurope'
}

resource db 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: 'pg'
  location: 'westeurope'
}
