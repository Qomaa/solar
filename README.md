# solar
Fetch output power from sun600g3-eu-230. Store output in a MariaDB. Provide stored data over a web server.
Self hosted: No cloud or app required

## Requirements
A running MariDB server.
.NET 7 runtime

# Usage

```
Usage:
-d, --dbserver: Server address of MySQL database, e.g.: 127.0.0.1
-u, --dbuser: user for MySQL database, e.g.: user
-p, --dbpassword: password for database user, e.g. secret
-n, --interval: interval in minutes to fetch and store to DB, default: 5
-s, --solarhost: hostname to solar host, e.g.: 192.68.178.47
-v, --solaruser: username for solarhost, e.g.: admin
-x, --solarpassword: password for username for solarhost, e.g.: admin
-f, --logdir: log directory. if none is provied or not able to write, logs to console
```
# Screenshots
