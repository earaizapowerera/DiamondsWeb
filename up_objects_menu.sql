-- ═══════════════════════════════════════════════════════════════
-- SQL para registrar pantallas de Diamonds Web en el menú de UserPortal
-- Ejecutar contra la base de datos UserPortal
-- ═══════════════════════════════════════════════════════════════

-- Obtener IDs de tipos de objeto
DECLARE @FolderTypeId INT = (SELECT object_type_id FROM dbo.up_object_types WHERE type_name = 'Carpeta del Menú');
DECLARE @ScreenTypeId INT = (SELECT object_type_id FROM dbo.up_object_types WHERE type_name = 'TransactionControl');
DECLARE @ViewTypeId INT = (SELECT object_type_id FROM dbo.up_object_types WHERE type_name = 'PowerView');

-- ═══════════════════════════════════════════════════════════════
-- CARPETAS DEL MENÚ (folders)
-- ═══════════════════════════════════════════════════════════════
DECLARE @FolderCatalogos UNIQUEIDENTIFIER = NEWID();
DECLARE @FolderVentas UNIQUEIDENTIFIER = NEWID();
DECLARE @FolderInventario UNIQUEIDENTIFIER = NEWID();
DECLARE @FolderProcesos UNIQUEIDENTIFIER = NEWID();
DECLARE @FolderConfig UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, is_active, created_at, updated_at)
VALUES
    (@FolderCatalogos, @FolderTypeId, 'Catálogos', NULL, 'fa-solid fa-book', 10, 1, GETUTCDATE(), GETUTCDATE()),
    (@FolderVentas, @FolderTypeId, 'Ventas', NULL, 'fa-solid fa-shopping-cart', 20, 1, GETUTCDATE(), GETUTCDATE()),
    (@FolderInventario, @FolderTypeId, 'Inventario', NULL, 'fa-solid fa-warehouse', 30, 1, GETUTCDATE(), GETUTCDATE()),
    (@FolderProcesos, @FolderTypeId, 'Procesos', NULL, 'fa-solid fa-gears', 40, 1, GETUTCDATE(), GETUTCDATE()),
    (@FolderConfig, @FolderTypeId, 'Configuración', NULL, 'fa-solid fa-sliders', 50, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- CATÁLOGOS
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, parent_id, is_active, created_at, updated_at)
VALUES
    (NEWID(), @ScreenTypeId, 'Diamantes', '/Catalogos/Diamantes', 'fa-solid fa-gem', 1, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Piezas Sencillas', '/Inventario/PiezasSencillas', 'fa-solid fa-ring', 2, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Piezas Compuestas', '/Inventario/PiezasCompuestas', 'fa-solid fa-puzzle-piece', 3, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Catálogo Repetidas', '/Catalogos/Repetidas', 'fa-solid fa-clone', 4, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Proveedores', '/Catalogos/Proveedores', 'fa-solid fa-truck', 5, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Razones Sociales', '/Catalogos/RazonesSociales', 'fa-solid fa-building', 6, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Grupos', '/Catalogos/Grupos', 'fa-solid fa-layer-group', 7, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Monedas', '/Catalogos/Monedas', 'fa-solid fa-coins', 8, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Tipos de Cambio', '/Catalogos/TiposCambio', 'fa-solid fa-exchange-alt', 9, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Opciones de Pago', '/Catalogos/OpcionesPago', 'fa-solid fa-credit-card', 10, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Divisores', '/Catalogos/Divisores', 'fa-solid fa-divide', 11, @FolderCatalogos, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- VENTAS / CONSULTAS
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, parent_id, is_active, created_at, updated_at)
VALUES
    (NEWID(), @ScreenTypeId, 'Punto de Venta', '/Ventas/PuntoDeVenta', 'fa-solid fa-cash-register', 1, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ViewTypeId, 'Consulta de Notas', '/Ventas/ConsultaNotas', 'fa-solid fa-file-invoice', 2, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ViewTypeId, 'Consulta de Bajas', '/Ventas/ConsultaBajas', 'fa-solid fa-arrow-down', 3, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Devoluciones Proveedor', '/Ventas/Devoluciones', 'fa-solid fa-undo', 4, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Devoluciones Cliente', '/Ventas/DevolucionesCliente', 'fa-solid fa-rotate-left', 5, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ViewTypeId, 'Consignación', '/Ventas/Consignacion', 'fa-solid fa-handshake', 6, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ViewTypeId, 'Control Anti-Lavado', '/AntiLavado', 'fa-solid fa-scale-balanced', 7, @FolderVentas, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- INVENTARIO
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, parent_id, is_active, created_at, updated_at)
VALUES
    (NEWID(), @ScreenTypeId, 'Inventario Físico', '/Inventario/InventarioFisico', 'fa-solid fa-clipboard-check', 1, @FolderInventario, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Transferencias', '/Inventario/Transferencias', 'fa-solid fa-exchange-alt', 2, @FolderInventario, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Registro Existencias', '/Inventario/Existencias', 'fa-solid fa-boxes-stacked', 3, @FolderInventario, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ViewTypeId, 'Reporte Faltantes', '/Inventario/Faltantes', 'fa-solid fa-magnifying-glass-minus', 4, @FolderInventario, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Lotes Repetidas', '/Inventario/LotesRepetidas', 'fa-solid fa-boxes-packing', 5, @FolderInventario, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- PROCESOS
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, parent_id, is_active, created_at, updated_at)
VALUES
    (NEWID(), @ScreenTypeId, 'Actualización Facturas', '/Procesos/ActualizacionFacturas', 'fa-solid fa-file-invoice-dollar', 1, @FolderProcesos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Actualización Pieza', '/Procesos/ActualizacionPieza', 'fa-solid fa-pen-to-square', 2, @FolderProcesos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Actualización Remisiones', '/Procesos/ActualizacionRemisiones', 'fa-solid fa-truck-ramp-box', 3, @FolderProcesos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Cambio de Status', '/Procesos/CambioStatus', 'fa-solid fa-arrows-rotate', 4, @FolderProcesos, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Pre Bajas', '/Procesos/PreBajas', 'fa-solid fa-clipboard-list', 5, @FolderProcesos, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- RECURSOS HUMANOS
-- ═══════════════════════════════════════════════════════════════
DECLARE @FolderRRHH UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, is_active, created_at, updated_at)
VALUES
    (@FolderRRHH, @FolderTypeId, 'Recursos Humanos', NULL, 'fa-solid fa-people-group', 45, 1, GETUTCDATE(), GETUTCDATE());

INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, parent_id, is_active, created_at, updated_at)
VALUES
    (NEWID(), @ViewTypeId, 'Equilibrio de Comisiones', '/RRHH/Equilibrio', 'fa-solid fa-scale-balanced', 1, @FolderRRHH, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- CONFIGURACIÓN
-- ═══════════════════════════════════════════════════════════════
INSERT INTO dbo.up_objects (object_id, object_type_id, object_name, route_path, icon_override, order_hint, parent_id, is_active, created_at, updated_at)
VALUES
    (NEWID(), @ScreenTypeId, 'Defaults Impuesto/Divisor', '/Configuracion/DefaultsImpuesto', 'fa-solid fa-percent', 1, @FolderConfig, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Defaults Utilidad', '/Configuracion/DefaultsUtilidad', 'fa-solid fa-chart-line', 2, @FolderConfig, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Defaults Utilidad Extra', '/Configuracion/DefaultsUtilidadExtra', 'fa-solid fa-plus-circle', 3, @FolderConfig, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Utilidad Extra Precio/Gramo', '/Configuracion/UtilidadPrecioGramo', 'fa-solid fa-weight-hanging', 4, @FolderConfig, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Tablas Jerarquías', '/Configuracion/Jerarquias', 'fa-solid fa-sitemap', 5, @FolderConfig, 1, GETUTCDATE(), GETUTCDATE()),
    (NEWID(), @ScreenTypeId, 'Diseño Etiquetas', '/Configuracion/DisenioEtiquetas', 'fa-solid fa-tag', 6, @FolderConfig, 1, GETUTCDATE(), GETUTCDATE());

-- ═══════════════════════════════════════════════════════════════
-- RESUMEN: 6 carpetas + 34 pantallas = 40 objetos de menú
-- ═══════════════════════════════════════════════════════════════
SELECT 'Menu objects created: ' + CAST(COUNT(*) AS VARCHAR) + ' items' FROM dbo.up_objects WHERE created_at >= DATEADD(MINUTE, -5, GETUTCDATE());
