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
