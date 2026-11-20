# Arias Empire ERP

Aplicación Blazor Web App para .NET 9 con renderizado interactivo de servidor, ASP.NET Core Identity y Entity Framework Core 9 sobre SQL Server. La base predeterminada usa LocalDB y las migraciones se aplican al iniciar.

## Requisitos

- .NET SDK 9.0 o posterior compatible con `net9.0`.
- SQL Server LocalDB (incluido con Visual Studio) o SQL Server / SQL Express.

## Crear, migrar y ejecutar

```powershell
dotnet new blazor -n AriasEmpireERP -o . -f net9.0 -int Server -au Individual -uld --all-interactive
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 9.0.22
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.22
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.22
dotnet tool install --global dotnet-ef --version 9.0.22
dotnet ef database update
dotnet run
```

Los paquetes ya están declarados en `AriasEmpireERP.csproj`; los comandos `dotnet add package` se muestran para recrear el proyecto desde cero. En los arranques posteriores, `DbInitializer` ejecuta `Database.MigrateAsync()` y verifica los datos iniciales.

La cadena predeterminada está en `appsettings.json` y apunta a `(localdb)\mssqllocaldb`. Para SQL Server/Express, establezca antes de ejecutar:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=.\SQLEXPRESS;Database=AriasEmpireERP;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
dotnet ef database update
dotnet run
```

Las migraciones están en `Data\Migrations`. Las áreas disponibles son administración y seguridad, proveedores, finanzas, RR. HH., inventario y CRM comercial. El menú y las rutas se restringen por rol; los documentos PDF de contratos se sirven mediante una ruta autorizada para RR. HH.

## Cuentas iniciales

El inicializador crea los siete roles y ocho cuentas solicitados. Contraseña inicial para cada cuenta: `Arias2026*`.

| Rol | Cuenta |
|---|---|
| SuperAdmin | `admin@ariasempire.com` |
| CoordinadorProveedores | `proveedores@ariasempire.com` |
| Tesorero | `tesoreria@ariasempire.com` |
| RRHH | `rrhh@ariasempire.com` |
| Almacenista | `almacen@ariasempire.com` |
| AsesorComercial | `asesor1@ariasempire.com`, `asesor2@ariasempire.com` |
| DirectorComercial | `director@ariasempire.com` |

Cambie las contraseñas iniciales antes de utilizar el sistema con datos de producción.

## Despliegue con Docker

Requiere Docker Desktop con Docker Compose. Compose inicia la aplicación y SQL Server Express; al arrancar, la aplicación aplica las migraciones automáticamente.

En PowerShell, desde la raíz del proyecto:

```powershell
Copy-Item .env.example .env
# Edite .env y reemplace MSSQL_SA_PASSWORD por una contraseña única y fuerte.
docker compose up --build -d
docker compose logs -f app
```

Abra `http://localhost:8080`. El puerto queda enlazado solo a la interfaz local para no exponer HTTP directamente a Internet; publique la aplicación mediante un proxy inverso con HTTPS. Los datos de SQL Server y los archivos cargados se conservan en volúmenes Docker (`sql_data` y `uploads`). `docker compose down` conserva esos volúmenes; `docker compose down -v` los elimina junto con los datos.

La conexión SQL usa cifrado con certificado confiable dentro de la red Compose. Para producción, proteja el acceso público con HTTPS mediante un proxy inverso y reemplace las contraseñas iniciales de Identity antes de exponer la aplicación. Si usa un SQL Server administrado, configure `ConnectionStrings__DefaultConnection` con su cadena de conexión y despliegue solo el servicio `app`; SQL Server Express incluido está pensado para despliegues pequeños y tiene sus límites de edición.
