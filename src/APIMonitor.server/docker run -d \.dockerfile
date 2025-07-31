docker run -d \
  --name mariadb_secure \
  -e MARIADB_ROOT_PASSWORD='ly8@01AM.zWgd' \
  -e MARIADB_DATABASE='apimonitor' \
  -p 127.0.0.1:3306:3306 \
  -v mariadb_data:/var/lib/mysql \
  --restart unless-stopped \
  mariadb:latest