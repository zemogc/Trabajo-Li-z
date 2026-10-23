# Changelog

Todos los cambios relevantes del proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto usa [versionado semántico](https://semver.org/lang/es/).

## [1.1.1] - 2026-10-23

### Corregido

- Columnas que se cortaban con la fuente real de Windows: fecha, estado y total en el historial de ventas; artículos, estado y medio de pago en Reportes; unidades e ingresos en los más vendidos. Las columnas de contenido corto (número, fecha, estado, cantidades e importes) ahora se ajustan a su contenido y el texto ocupa el espacio restante.
- El texto de ayuda de las observaciones en Entradas de mercancía no cabía en el campo.

## [1.1.0] - 2026-10-23

### Agregado

- **Recibos:** vista previa e impresión del recibo de cualquier venta (botón *Recibo* o doble clic en el historial), en formato de impresora térmica de 80 mm. Los datos de la tienda se configuran en la sección `Store` de `appsettings.json`.
- **Reportes:** pantalla para consultar cualquier período, con totales, todas las ventas (las anuladas aparecen tachadas y no suman), ventas por medio de pago, los 20 productos más vendidos y exportación a CSV.
- **Log diario en archivo** en `%LOCALAPPDATA%\Supermarket\logs`, que se conserva 14 días. Los mensajes de error indican su ubicación.

## [1.0.0] - 2026-10-23

Primera versión publicada. Reemplaza por completo el proyecto universitario original, que no compilaba.

### Agregado

- **Inicio:** ventas, ingresos y ticket promedio del día; productos más vendidos de los últimos 30 días; ventas del día por medio de pago, y productos por reponer.
- **Ventas:** carrito con validación de stock, medio de pago, total e historial con el detalle de cada venta. Cada venta se registra en una transacción con descuento de stock atómico, así que no hay sobreventa entre cajas concurrentes.
- **Anulación de ventas:** devuelve las unidades al inventario una sola vez, incluso con anulaciones simultáneas.
- **Entradas de mercancía:** registro de lo recibido por proveedor, con costo unitario y observaciones.
- **Productos:** categoría, proveedor habitual opcional, stock mínimo y resaltado de los productos por reponer.
- Mantenimiento de categorías, proveedores y medios de pago.
- Búsqueda por código o nombre, ordenamiento por columnas y exportación a CSV compatible con Excel, con protección contra inyección de fórmulas.
- Creación y actualización automática del esquema de base de datos al iniciar, con datos de ejemplo opcionales.
- Paquete autocontenido para Windows x64 que no requiere instalar .NET.

### Técnico

- Arquitectura MVP (Passive View) en tres proyectos: `Core`, `Data` y `WinForms`.
- .NET 10, Dapper y SQL Server, con configuración, inyección de dependencias y logging del host genérico.
- 159 pruebas: unitarias, de renderizado de pantallas e integración contra SQL Server 2022 con Testcontainers.
- `build.ps1` para verificar en local (formato, build, pruebas y cobertura) y `publish.ps1` para generar el paquete.

[1.1.1]: https://github.com/zemogc/Trabajo-Li-z/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/zemogc/Trabajo-Li-z/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/zemogc/Trabajo-Li-z/releases/tag/v1.0.0
