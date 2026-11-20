# **VmtFabric** 

### Documento de requerimientos — Sistema de gestión de talento de la fábrica de software 

|1.1 Alcance........................................................................................................................................1|
|---|
|1.2 Actores y roles.............................................................................................................................2|
|1.3 Estructura de navegación............................................................................................................2|
|2. Requerimientos funcionales..............................................................................................................2|
|2.1 Transversales..............................................................................................................................2|
|2.2 Dashboard...................................................................................................................................2|
|2.3 Colaboradores.............................................................................................................................3|
|2.4 Clientes........................................................................................................................................4|
|2.5 Plan de carrera............................................................................................................................4|
|2.6 Evaluaciones................................................................................................................................4|
|2.7 Análisis.........................................................................................................................................5|
|2.8 Reportes......................................................................................................................................5|
|2.9 Gestión de usuarios.....................................................................................................................5|
|2.10 Catálogo de tecnologías............................................................................................................5|
|3. Reglas de negocio.............................................................................................................................5|
|4. Requerimientos no funcionales.........................................................................................................6|
|5. Modelo de datos propuesto...............................................................................................................6|



#### **1.1 Alcance** 

VmtFabric centraliza la gestión del talento de una fábrica de software: colaboradores, clientes a los que se asignan, contratos, evaluaciones técnicas, plan de carrera, inventario de tecnologías, reportes y administración de usuarios. 

VmtFabric · Requerimientos · Pág. 1 

#### **1.2 Actores y roles** 

|**Rol**|**Descripción (a validar)**|
|---|---|
|Admin|Acceso total, incluida la gestión de usuarios y catálogos.|
|Gestor|Gestiona colaboradores, clientes, asignaciones, documentos y<br>evaluaciones.|
|Consulta|Solo lectura de dashboard, fichas, análisis y reportes.|



#### **1.3 Estructura de navegación** 

|**Sección**|**Módulo**|**Ruta**|
|---|---|---|
|General|Dashboard|/|
|Talento|Colaboradores|/colaboradores, /colaboradores/:id|
|Talento|Clientes|/clientes, /clientes/:id|
|Talento|Plan de Carrera|/plan-carrera|
|Talento|Evaluaciones|/evaluaciones|
|Inteligencia|Análisis|/analisis|
|Inteligencia|Reportes|/reportes|
|Administración|Gestión de usuarios|/usuarios|
|Administración|Catálogo de Tecnologías|/tecnologias|



## **2. Requerimientos funcionales** 

#### **2.1 Transversales** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-01|Inicio y cierre de sesión; el header muestra nombre, correo y rol del<br>usuario.|Alta|
|RF-02|Menú lateral agrupado en General, Talento, Inteligencia y<br>Administración, visible según el rol.|Alta|
|RF-03|Búsqueda contextual en el módulo activo.|Alta|
|RF-04|Campana de notificaciones con contratos próximos a vencer (contador<br>y enlace a la ficha).|Alta|
|RF-05|Diseño responsive con menú colapsable en móvil.|Media|



#### **2.2 Dashboard** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-06|Indicadores: total de colaboradores, asignados (con % del total), en<br>banca y avance promedio del plan de carrera (solo activos).|Alta|
|RF-07|Gráfico de barras de colaboradores por cliente.|Alta|



VmtFabric · Requerimientos · Pág. 2 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-08|Gráfico de dona de distribución por estado.|Alta|
|RF-09|Gráfico de tecnologías más frecuentes (top 10).|Alta|
|RF-10|Gráfico de colaboradores por nivel (Junior, Semi Senior, Senior, Lead).|Alta|
|RF-11|Lista de contratos próximos a vencer con días restantes, cliente y<br>cargo; cada ítem abre la ficha.|Alta|



#### **2.3 Colaboradores** 

##### **Listado** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-12|Listado paginado con búsqueda por nombre, cédula o correo.|Alta|
|RF-13|Filtros por estado con contador: Todos, Asignado, Activo, En banca,<br>Desvinculado.|Alta|
|RF-14|Columnas: colaborador (iniciales, nombre, CI, correo), asignación<br>(cliente, cargo, nivel), estado, fin de contrato con alerta “Vence en X<br>días”.|Alta|
|RF-15|Acciones por fila: Ver ficha, Editar, Eliminar (con confirmación).|Alta|
|RF-16|Crear nuevo colaborador (en la plantilla no está implementado).|Alta|



##### **Ficha del colaborador** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-17|Cabecera con nombre, CI, edad, estado, cliente y cargo/nivel, y<br>acciones Editar, Nueva asignación y Exportar ficha.|Alta|
|RF-18|Pestaña Contacto: cédula, edad, correo, celular, estado.|Alta|
|RF-19|Gestión de documentos del colaborador: subir, descargar y eliminar<br>(CV, cédula, contrato) con tipo y fecha.|Alta|
|RF-20|Pestaña Asignación actual: cliente, cargo, nivel, líder, ingreso al<br>cliente, fin de contrato, calificación (/100) con fecha de última<br>evaluación, funciones al inicio y actuales, tecnologías usadas; acción<br>Actualizar asignación.|Alta|
|RF-21|Pestaña Historial: asignaciones pasadas con cliente, cargo, nivel,<br>calificación, fechas, duración, líder y tecnologías; filtro por cliente y<br>orden.|Alta|
|RF-22|Pestaña Plan de carrera: avance %, fecha de asignación, próximo<br>objetivo y enlace a VmtSkills (solo lectura).|Alta|
|RF-23|Pestaña Tecnologías: skills consolidados con nivel 1–5 (Básico…<br>Experto) y clientes donde se usaron; acción Agregar.|Alta|
|RF-24|Exportar ficha del colaborador (formato a definir: PDF).|Media|



VmtFabric · Requerimientos · Pág. 3 

##### **Formularios** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-25|Editar colaborador: nombre*, cédula*, edad*, correo*, celular, estado,<br>próximo objetivo. Cliente y cargo no se editan aquí.|Alta|
|RF-26|Nueva asignación: cliente*, líder, cargo*, nivel*, fecha de ingreso*, fin<br>de contrato, tecnologías, funciones al inicio, funciones actuales,<br>calificación (0–100).|Alta|



#### **2.4 Clientes** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-27|Vista en tarjetas o lista, con búsqueda; cada cliente muestra industria,<br>colaboradores activos, roles y vencimiento más próximo.|Alta|
|RF-28|Crear cliente: nombre*, industria/sector, responsable, teléfono, correo*,<br>inicio de relación, tarifa mensual (USD).|Alta|
|RF-29|Detalle – General: contacto (responsable, correo, teléfono) y relación<br>comercial (inicio, tarifa mensual referencial).|Alta|
|RF-30|Detalle – Financiero: facturado últimos 12 meses, promedio mensual,<br>colaboradores activos y gráfico de facturación mensual.|Alta|
|RF-31|Detalle – Activos: colaborador, rol, seniority, desde, hasta, estado.|Alta|
|RF-32|Detalle – Historial: colaboradores que salieron con rol, fechas y motivo.|Alta|
|RF-33|Detalle – Contratos: subir contrato con etiqueta; listado con archivo,<br>etiqueta, fecha de carga, tamaño y acciones (en la plantilla es<br>simulado).|Alta|



#### **2.5 Plan de carrera** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-34|Avance promedio de la fábrica.|Alta|
|RF-35|Tarjeta por colaborador con cargo/nivel, barra de avance y próximo<br>objetivo.|Alta|
|RF-36|Datos sincronizados desde VmtSkills (sistema externo); el módulo es<br>de solo lectura.|Media|



#### **2.6 Evaluaciones** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-37|Tabla: colaborador, cargo, prueba, fecha de última evaluación y<br>calificación con color por rango.|Alta|
|RF-38|Detalle en modal: código (p. ej. EV-2026-310), fecha, calificación,<br>evaluador, duración.|Alta|
|RF-39|Temas evaluados con correctas/total y %; cada tema despliega<br>preguntas de opción múltiple con la respuesta entregada y si fue<br>correcta.|Alta|



VmtFabric · Requerimientos · Pág. 4 

#### **2.7 Análisis** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-40|Evaluación promedio de la fábrica.|Alta|
|RF-41|Colaboradores por cliente (barras).|Alta|
|RF-42|Estado del talento (pastel).|Alta|
|RF-43|Distribución por nivel (barras).|Alta|
|RF-44|Cobertura de tecnologías (radar).|Alta|



#### **2.8 Reportes** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-45|Exportar Colaboradores por cliente (asignaciones vigentes).|Alta|
|RF-46|Exportar Contratos por vencer (60 días).|Alta|
|RF-47|Exportar Skills disponibles.|Alta|
|RF-48|Exportar Avance de plan de carrera.|Alta|
|RF-49|Exportar Evaluaciones del período.|Alta|
|RF-50|Exportar Colaboradores en banca.|Alta|



#### **2.9 Gestión de usuarios** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-51|Listado con nombre, correo, rol (Admin, Gestor, Consulta) y estado<br>(Activo/Inactivo).|Alta|
|RF-52|Crear, editar, activar/desactivar usuarios y asignar rol (en la plantilla no<br>está implementado).|Alta|



#### **2.10 Catálogo de tecnologías** 

|**ID**|**Requerimiento**|**Prioridad**|
|---|---|---|
|RF-53|Ranking de tecnologías por número de colaboradores.|Alta|
|RF-54|Tarjeta por tecnología con número de colaboradores, nivel promedio y<br>lista de colaboradores con su nivel.|Alta|
|RF-55|CRUD del catálogo de tecnologías (solo Admin).|Media|



## **3. Reglas de negocio** 

|**ID**|**Regla**|
|---|---|
|RN-01|Un colaborador tiene como máximo una asignación activa.|
|RN-02|Al crear una nueva asignación, la activa se cierra y pasa al historial.|
|RN-03|Si las fechas se traslapan, se advierte que la asignación activa se cerrará|



VmtFabric · Requerimientos · Pág. 5 

|**ID**|**Regla**|
|---|---|
||anticipadamente y se exige confirmación explícita.|
|RN-04|La cédula es única y debe validarse con el algoritmo de cédula ecuatoriana.|
|RN-05|Estados del colaborador: Activo, Asignado, En banca, Desvinculado.|
|RN-06|Niveles: Junior, Semi Senior, Senior, Lead. Nivel de tecnología: 1 a 5.|
|RN-07|Calificaciones en escala 0–100.|
|RN-08|Contrato “próximo a vencer” según un umbral configurable (alerta ~30 días; reporte<br>60 días).|
|RN-09|Eliminar un colaborador es borrado lógico o pasa a Desvinculado, conservando el<br>historial.|
|RN-10|La edad se calcula a partir de la fecha de nacimiento (recomendado, no se<br>almacena).|



## **4. Requerimientos no funcionales** 

|**ID**|**Requerimiento**|
|---|---|
|RNF-01|Autenticación con JWT (access + refresh token) y contraseñas con hash.|
|RNF-02|Autorización por roles en la API ([Authorize(Roles=…)]) y guards en Angular.|
|RNF-03|Validación de datos en frontend (Reactive Forms) y backend<br>(FluentValidation/DataAnnotations).|
|RNF-04|Almacenamiento de archivos en disco o blob storage; límite de tamaño y tipos<br>permitidos (PDF, imágenes).|
|RNF-05|Paginación, filtros y ordenamiento del lado del servidor.|
|RNF-06|Interfaz responsive y accesible (navegación por teclado, etiquetas ARIA).|
|RNF-07|Auditoría básica: fecha y usuario de creación/modificación.|
|RNF-08|Documentación de la API con Swagger/OpenAPI.|



## **5. Modelo de datos propuesto** 

|**Entidad**|**Atributos principales**|
|---|---|
|Colaborador|Id, Nombre, Cedula (única), FechaNacimiento, Correo, Celular,<br>Estado, ProximoObjetivo|
|Cliente|Id, Nombre, Industria, Responsable, Telefono, Correo, InicioRelacion,<br>TarifaMensual|
|Asignacion|Id, ColaboradorId, ClienteId, Lider, Cargo, Nivel, FechaIngreso,<br>FinContrato, FechaSalida, Calificacion, FuncionesInicio,<br>FuncionesActuales, EsActual, MotivoSalida|
|Tecnologia|Id, Nombre|
|ColaboradorTecnologia|ColaboradorId, TecnologiaId, Nivel (1–5)|



VmtFabric · Requerimientos · Pág. 6 

|**Entidad**|**Atributos principales**|
|---|---|
|AsignacionTecnologia|AsignacionId, TecnologiaId|
|Documento|Id, Propietario (ColaboradorId o ClienteId), Tipo/Etiqueta,<br>NombreArchivo, Ruta, Tamano, FechaCarga|
|Evaluacion|Id, Codigo, ColaboradorId, Prueba, Evaluador, Fecha, DuracionMin,<br>Calificacion|
|EvaluacionTema /<br>Pregunta / Opcion /<br>Respuesta|Temas, preguntas de opción múltiple, opción correcta y respuesta<br>entregada|
|PlanCarrera|ColaboradorId, Avance, FechaAsignacion, ProximoObjetivo,<br>UltimaSincronizacion|
|Facturacion|Id, ClienteId, Periodo (AAAA-MM), Monto|
|Usuario|Id, Nombre, Correo, PasswordHash, Rol, Activo|



VmtFabric · Requerimientos · Pág. 7 

