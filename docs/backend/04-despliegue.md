# Despliegue en MonsterASP.NET (plan Free)

Decisión registrada en DT-12. Horizonte: el proyecto debe funcionar al menos **hasta enero de 2027**. Plan gratuito: apto para desarrollo, pruebas y demo, no para producción con muchos usuarios.

| Etapa | Qué se sube | Cuándo |
|---|---|---|
| **1** | Solo la base de datos (MonsterASP) — el equipo desarrolla con la API en local | **Ahora** |
| **2** | La API a MonsterASP (mismo proveedor que la BD) | Cuando el backend esté terminado |
| **3** | El frontend Angular como sitio estático en Render | Cuando el frontend esté terminado |

```
Etapa 1:  [PC de cada integrante: API local] ──acceso remoto, cifrado──> [BD MonsterASP]

Etapa 2+: [Angular en Render Static Site] ──HTTPS──> [API MonsterASP] ──conexión interna──> [BD MonsterASP]
          [Equipo en desarrollo] ──acceso remoto──────────────────────────────────────────> [BD MonsterASP]
```

---

## Etapa 1 · Base de datos compartida (ahora)

### Crear la base (lo hace una sola persona)
1. Crear cuenta en monsterasp.net (plan **Free**, no pide tarjeta). Conviene usar una cuenta del equipo, no personal.
2. Panel → **Databases → Create database** → **SQL Server 2025**, centro de datos **EU** (el único del plan Free). El nombre lo asigna MonsterASP (`dbXXXX`).
3. Abrir la base → **Users → Remote access → Enabled**. Sin esto solo acepta conexiones de sitios alojados en MonsterASP.
4. Copiar la cadena **remota**: el servidor contiene `public` (p. ej. `dbXXXX.public.databaseasp.net`); nunca usar la IP. Probar con **Test connection**.
5. Ejecutar el esquema **una sola vez**, conectado a esa base:
   ```bash
   sqlcmd -S dbXXXX.public.databaseasp.net -d dbXXXX -U dbXXXX -P "<password>" -N -b -i bd/VmtFabric_BD.sql
   ```
   O abrir el archivo en SSMS (Encryption: Mandatory) con la base `dbXXXX` seleccionada.

### Conectarse desde el PC de cada integrante (archivo `.env`)
Base del equipo: servidor `db81318.public.databaseasp.net`, base y usuario `db81318`, acceso remoto **habilitado**.

La contraseña **nunca** se guarda en el repositorio ni se pega en chats en texto plano; se comparte por un gestor de contraseñas. Cada integrante:
```bash
cd backend
copy .env.example .env      # completar DB_PASSWORD y JWT_KEY
dotnet run --launch-profile http
# Comprobar: http://localhost:5222/health  ->  Healthy
```
- `backend/.env` está en `.gitignore` y `.dockerignore`; la plantilla versionada es `backend/.env.example`.
- La API lo lee **solo en Development** (`Extensions/DotEnvExtensions.cs`) y arma la cadena con `SqlConnectionStringBuilder` (TLS obligatorio con validación de certificado). Si falta una clave, la API no arranca y dice cuál falta, sin mostrar valores.
- Sin `.env`, la API usa la base local de `appsettings.Development.json`.

### Si no hay conexión desde el PC
1. Panel → la base → **Users → Remote access → Test connection**. Si da OK, el servidor está bien y el problema es la red local.
2. Panel → **Automated Security Protection**: muestra si la IP fue bloqueada (24 h) y permite quitar el bloqueo.
3. Probar desde otra red (p. ej. datos móviles) para saber si es la red/ISP.
4. Mientras tanto, el script se puede ejecutar desde el panel (**Run T-SQL**, **Import SQL** o **WebMSSQL**), que corre dentro de MonsterASP.

### Reglas de trabajo con una base compartida
- **Cambios de esquema:** el script `bd/VmtFabric_BD.sql` crea todo desde cero y no se vuelve a ejecutar sobre la base compartida. Cada cambio posterior va en un script incremental numerado (`bd/cambios/001_descripcion.sql`, `002_...`), se revisa en equipo, se ejecuta una sola vez y se refleja también en el script principal y en las entidades/configuraciones EF.
- **Datos de prueba:** cualquiera puede ver y modificar los datos de los demás. No usar datos personales reales.
- **Bloqueo por intentos fallidos:** varios errores de contraseña bloquean la IP 24 h. Copiar la cadena exacta, no escribirla a mano.
- **Copias de seguridad:** el plan Free no las hace. Exportar una copia **cada semana** y antes de cada cambio de esquema.

---

## Etapa 2 · API en MonsterASP (cuando el backend esté terminado)

1. Panel → **Websites → Create website** (el plan Free permite **un** sitio; lo ocupa la API). Subdominio `*.runasp.net`.
2. **Scripting:** seleccionar ASP.NET Core con el runtime de .NET 10.
3. Publicar: desde Visual Studio (perfil de publicación de MonsterASP), Web Deploy o FTP.
4. Configurar en el sitio, **fuera del repositorio**:
   | Clave | Valor |
   |---|---|
   | `ConnectionStrings:VmtFabric` | Cadena **interna** de la base (sin `public`): no sale a internet |
   | `AllowedHosts` | `vmtfabric.runasp.net` (el subdominio elegido) |
   | `Cors:AllowedOrigins:0` | URL del frontend en Render |
   | `Jwt:Key` | Clave aleatoria de 48+ bytes en Base64, **distinta** de las de desarrollo |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   El mecanismo exacto (panel o `web.config` generado al publicar) se define en esa fase.
5. **Pedir HTTPS por ticket a soporte** apenas se suba el sitio (en Free no es automático). Sin HTTPS no se puede usar la API: JWT y contraseñas viajarían en claro.
6. `ReverseProxy:Enabled` queda en `false`: IIS no actúa como proxy con cabeceras reenviadas como Render.

## Etapa 3 · Frontend en Render (Static Site)
- Render Static Site es gratis y **no se duerme**.
- El proyecto Angular está creado con SSR (`server.ts`); para un sitio estático debe compilarse sin servidor (prerender/solo navegador).
- La URL de la API se configura en el `environment` de producción de Angular.
- `render.yaml` se reutilizará para este sitio estático.

## Plan B · API en Render (Docker)
Si MonsterASP no sirviera para la API, el repositorio ya tiene `backend/Dockerfile`, `backend/.dockerignore` y `render.yaml` preparados para desplegar la API en Render con Docker (región Frankfurt, `ReverseProxy__Enabled=true`, `AllowedHosts=*.onrender.com`). Verificado en simulación local; la imagen Docker no se construyó aún. Desventajas frente a MonsterASP: se duerme a los 15 min, disco efímero y la BD queda en otro proveedor.

---

## Limitaciones del plan Free de MonsterASP

| Limitación | Impacto | Mitigación |
|---|---|---|
| Sin copias de seguridad automáticas | Pérdida del trabajo del equipo | Copia semanal manual y antes de cambios de esquema. |
| Archiva servicios sin uso durante 6 meses | — | No afecta hasta enero. |
| Base accesible desde internet, sin filtro de IP | Fuerza bruta | Contraseña larga y aleatoria; compartirla solo por gestor de contraseñas; rotarla si se filtra. Desactivar el acceso remoto cuando el equipo termine. |
| No envía correos | Sin "olvidé mi contraseña" por email | **Fase 02:** el Administrador restablece con contraseña temporal (`DebeCambiarPassword`). |
| Sin tareas SQL programadas | `sp_GenerarAvisos` no corre a diario | **Fase 10:** generar avisos bajo demanda. |
| El sitio se duerme tras 3 h sin tráfico | La primera petición tarda más | Aceptable. |
| 256 MB de RAM, CPU compartida | Rendimiento limitado | Suficiente para el equipo y una demo. |
| HTTPS manual (ticket a soporte) | Sin HTTPS la API es insegura | Pedirlo apenas se publique la API. |
| Un solo sitio web | El frontend no cabe | Frontend en Render Static Site. |
| Disco de 5 GB por sitio | — | **Fase 07:** guardar documentos en el disco del sitio (confirmar permisos de escritura al desplegar). |
