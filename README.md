# NetResVM
[![Unit and Integration Tests](https://github.com/maty5302/NetResVM/actions/workflows/tests.yml/badge.svg)](https://github.com/maty5302/NetResVM/actions/workflows/tests.yml)
[![Build and Push Docker Image](https://github.com/maty5302/NetResVM/actions/workflows/docker.yml/badge.svg)](https://github.com/maty5302/NetResVM/actions/workflows/docker.yml)

🇬🇧 [English](README.md)
🇨🇿 [Čeština](README.cs.md)

This system is used by students to manage reservations of lab environments (Cisco CML and EVE-NG), allows backups to be created and restored, and facilitates coordination between users. The application is designed with simplicity, security, and efficiency in mind.

## License

This project is available under the [GNU GPL v3](LICENSE) license.

## System requirements
- Access to CML and EVE-NG servers
- Operating system Linux or Windows
- Docker 
- .NET 10.0 Runtime
- ASP.NET 10.0 Runtime
- Microsoft SQL Server 2022
- libldap - library for LDAP auth (Linux only)

## Installation Docker (Recommended)
> Note: for installation in docker you don't need requirements from above except from Docker.
#### 1. Download and install Docker
#### 2. Download [docker-compose.yml](docker/docker-compose.yml) from the `docker` folder (or from [GitHub Releases](https://github.com/maty5302/NetResVM/releases))
#### 3. Create [.env](docker/.env.example) file with following
> Choose your own strong password for database
```
DB_USER=sa
DB_PASSWORD=vase_silne_heslo
```
#### 4. Run docker-compose.yml in background and wait for about 1-2 minutes.
```bash
docker compose pull && docker compose up -d
```
> **Automatic Start on PC Reboot:**
> The containers use the `restart: always` policy, so they will automatically restart in the background whenever your system boots up. For this to work, ensure the Docker daemon itself is configured to start on boot:
> - **Linux:** `sudo systemctl enable --now docker` (and optionally `sudo systemctl enable --now containerd`)
> - **Windows:** In Docker Desktop (*Settings -> General*), enable *Start Docker Desktop when you sign in*.


#### 5. You can use the app usually hosted on http://localhost:8080/ and sign in with default login=admin and password=Password123.
>Note after first login you should change the password for a stronger one!!!

> [!WARNING]
> **Database Access from Host:**
> By default, the MS SQL database port (`1433`) is not exposed outside the Docker network as the application communicates with it internally. If you need direct database access from the host system (e.g. via SSMS or DBeaver), you can add the following under the `mssql` service in `docker-compose.yml`:
> ```yaml
> ports:
>   - "1433:1433" # or for better security bind to localhost only: "127.0.0.1:1433:1433"
> ```
> **Warning:** Exposing port `1433` on a server or public network without firewall restrictions is a security risk. Ensure you use a strong `DB_PASSWORD`.

> [!NOTE]
> **Docker Operation and Reverse Proxy:**
> - **Permissions and Security:** The application container runs as a non-root user `app` (UID 1654). The helper service `init-permissions` automatically sets proper ownership on `./backups`, `./logs`, and session data protection keys (`dp-keys`).
> - **Telnet:** Port `2323` is restricted to localhost only (`127.0.0.1:2323`) for security reasons.
> - **Reverse Proxy (HTTPS):** When deploying behind a reverse proxy (e.g. Nginx, Caddy, Traefik, Cloudflare), configure the trusted proxy IP or subnet in the `netresvm` service `environment` section, e.g. `- ReverseProxy__KnownNetworks__0=172.16.0.0/12`. Without this setting, `X-Forwarded-For` headers are ignored to prevent IP spoofing, which would cause rate limiting and lockout to block the reverse proxy's IP.


## Installation without Docker
#### 1. Download and unzip the archive for your operating system from [GitHub Releases](https://github.com/maty5302/NetResVM/releases)

#### 2. Install packages

Need to install all system requirements with your package manager before getting to next step.

```bash
mssql-server
mssql-tools
unixodbc-dev
dotnet-sdk-10.0
aspnetcore-runtime-10.0
dotnet-runtime-10.0
libldap 2.5-0 or openldap 2.5-0
```
#### 3. Create tables in database
 After installing all packages make sure your MS SQL Server is running and have installed mssql-tools. You need to run following command to insert tables to database (the application also performs automatic schema migrations on startup to apply any missing updates): 
```bash
sqlcmd -S IPaddress -U Username -P "YourPassword" -i SQLCreateTablesBc.sql
```

#### 4. Setup connection

In app directory find a file called `sqlconnection.json` and fill all connection information.

```json
{
  "DataSource": "",
  "UserID": "",
  "Password": ""
}
```

#### 5. Run application

Linux
```bash
chmod +x NetResVM
./NetResVM
```

Windows
```powershell
.\NetResVM.exe
```

## Features

### Automatic starting and stopping servers

The system automatically starts the reserved laboratory server shortly before the reservation begins and shuts it down after the reservation ends. This saves server resources and ensures smooth operation.

### Creating reservation

Users can create reservations for virtual lab environments (Cisco CML or EVE-NG) by selecting the desired date, time slot, and server. The reservation system ensures there are no conflicts and displays current availability in real-time.

1. From main menu click on topbar Reservation
2. Create reservation
3. Fill the form and click make reservation

![Create reservation example](assets/readme/create-reservation-example.png)

### Backups of laboratory

Users can create snapshots (backups) of their active lab environments. These backups can be downloaded or stored on the server and later restored to continue work without data loss.

1. Select server and after that laboratory you want to backup
2. Click on make a backup
3. After making backup you will be forwarded to page for viewing all backups.

### Assign laboratory

1. Select server and after that laboratory you want to assign to yourself
2. Click on own a lab

After that you can see it in My labs page and you can start and stop the lab as well. 

### Adding and removing CML and EVE-NG server connection
Administrators can add new CML or EVE-NG servers into the system by providing connection details such as IP address, authentication method, and server type. Servers can also be removed. This ensures scalability and flexibility in managing lab infrastructure.

1. From main page there is a + button for adding new server
2. Fill the form and click add server

Deleting can be done from the main page, and for editing a server there is a button that redirects to a form with pre-filled server information. 

![Add server connection page](assets/readme/create-server-connection.png)
### Managing users
System administrators have the ability to manage user accounts. This includes creating, editing, temporarily disabling and deleting user profiles. 

1. From any page click on your username that takes you to settings page
2. For creating user there is form and for managing existing users there is button manage users
3. Clicking on that button takes you to manage users page where you can either delete or deactivate/activate user. 

![Manage user page](assets/readme/manage-users.png)

### Server selection with Online status
The Server Selection dashboard acts as the main entry point for users to connect to their virtualization environments (Cisco CML or EVE-NG). It provides a clear, card-based overview of all registered servers.

* **Real-time Status Monitoring:** Each server card displays a dynamic `Online` or `Offline` status badge, powered by asynchronous background ping checks to prevent users from attempting to connect to dead servers.
* **Platform Identification:** Instantly identifies the underlying virtualization platform (e.g., CML or EVE) for each node.
* **Quick Administrative Actions:** Admins have direct access to edit configurations or delete server connections directly from the grid using intuitive icon buttons.
* **Seamless Navigation:** A primary "Connect to server" button routes the user directly to the selected server's lab environment.
* **Easy Expansion:** A dedicated, full-width action area at the bottom allows administrators to quickly add new server connections to the infrastructure.

![Server Selection Home](assets/readme/server-selection-online-status.png)

### Managing collaborators of lab
Users who own a laboratory can invite other registered users to collaborate on their network topologies. This feature is ideal for group projects and team assignments, allowing multiple students to share access to the same lab environment, manage its state (start/stop), and work together seamlessly during reserved time slots.

1. Navigate to the details of lab you own.

2. Click on the "Manage Collaborators" (or similar share icon) button on a specific lab you want to share.

3. Search for the desired user by their username, and click to add them as a collaborator.

4. To revoke access once the joint work is finished, simply click the remove button next to an existing collaborator's name in the same menu.

> By default, the primary owner (the user who claimed the lab first) can add or remove collaborators.

![Manage collaborators menu](assets/readme/manage-collaborators.png)

## Security and Session Management

The application implements multiple security layers and operational hardening:

* **Brute-Force Protection & Account Lockout:** Failed login attempt tracking (`LoginAttemptTracker`). After 5 consecutive failed attempts for a specific username and IP address, the account is temporarily locked for 5 minutes.
* **Rate Limiting:** The login endpoint is protected by a fixed-window rate limiter with a maximum of 30 requests per minute per client IP (returns HTTP 429 Too Many Requests when exceeded).
* **Session and Cookie Hardening:** Authentication and session cookies are configured with `HttpOnly`, `SameSite=Lax`, and a 30-minute idle expiration. Deactivating a user immediately invalidates their active authentication cookie on the next request (`OnValidatePrincipal`).
* **Security HTTP Headers:** Every HTTP response includes `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`, and `Referrer-Policy: strict-origin-when-cross-origin`.
* **Multi-Server Concurrency:** Cisco CML and EVE-NG adapters isolate authentication sessions and tokens per server ID. Laboratory ownership uses a composite key `(LabID, ServerID, UserID)`, eliminating collisions between multiple virtualization servers with identical lab IDs.
* **Automatic Database Migrations:** The application automatically verifies schema integrity and applies missing idempotent migrations on startup (such as adding the `OwnershipID` column), eliminating manual schema maintenance on existing databases.
