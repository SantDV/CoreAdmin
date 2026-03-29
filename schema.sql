BEGIN TRANSACTION;
CREATE TABLE IF NOT EXISTS "CATEGORIA_PRODUCTO" (
	"id_categoria"	INTEGER,
	"nombre_categoria"	TEXT(50) NOT NULL,
	"descripcion"	TEXT,
	"estado"	INTEGER DEFAULT 1,
	"fecha_registro"	TEXT DEFAULT (datetime('now', 'localtime')),
	PRIMARY KEY("id_categoria" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "CLIENTE" (
	"id_cliente"	INTEGER,
	"documento"	TEXT,
	"nombre"	TEXT(50),
	"apellido"	TEXT(50),
	"fecha_nacimiento"	TEXT,
	"id_genero"	INTEGER,
	"direccion"	TEXT(50),
	"telefono"	TEXT(20),
	"email"	TEXT(50),
	"id_plan"	INTEGER,
	"fecha_inicio"	TEXT,
	"fecha_vencimiento"	TEXT,
	"fecha_registro"	TEXT DEFAULT (datetime('now', 'localtime')),
	"estado"	INTEGER DEFAULT 1,
	"nota_adicional"	TEXT,
	"huella"	TEXT,
	"tipo_sangre"	TEXT,
	"alergias"	TEXT,
	"enfermedades_cronicas"	TEXT,
	"contacto_emergencia_nombre"	TEXT,
	"contacto_emergencia_telefono"	TEXT,
	"vencimiento_apto_medico"	TEXT,
	PRIMARY KEY("id_cliente" AUTOINCREMENT),
	FOREIGN KEY("id_genero") REFERENCES "GENERO"("id_genero"),
	FOREIGN KEY("id_plan") REFERENCES "PLANES"("id_plan")
);
CREATE TABLE IF NOT EXISTS "DETALLE_VENTA" (
	"id_detalle"	INTEGER,
	"id_venta"	INTEGER NOT NULL,
	"id_producto"	INTEGER NOT NULL,
	"cantidad"	INTEGER NOT NULL,
	"precio_unitario"	NUMERIC(10, 2) NOT NULL,
	"subtotal"	NUMERIC(10, 2) NOT NULL,
	PRIMARY KEY("id_detalle" AUTOINCREMENT),
	FOREIGN KEY("id_producto") REFERENCES "PRODUCTO"("id_producto"),
	FOREIGN KEY("id_venta") REFERENCES "VENTA"("id_venta")
);
CREATE TABLE IF NOT EXISTS "EMPLEADO" (
	"id_empleado"	INTEGER,
	"documento"	TEXT,
	"nombre"	TEXT,
	"apellido"	TEXT,
	"direccion"	TEXT,
	"telefono"	TEXT,
	"id_usuario"	INTEGER,
	"estado"	INTEGER DEFAULT 1,
	"fecha_registro"	TEXT DEFAULT (datetime('now', 'localtime')),
	PRIMARY KEY("id_empleado" AUTOINCREMENT),
	FOREIGN KEY("id_usuario") REFERENCES "USUARIO"("id_usuario")
);
CREATE TABLE IF NOT EXISTS "GENERO" (
	"id_genero"	INTEGER,
	"genero_nombre"	text(20),
	PRIMARY KEY("id_genero" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "METODO_PAGO" (
	"id_metodo_pago"	INTEGER,
	"nombre_metodo"	TEXT(30) NOT NULL,
	"estado"	INTEGER DEFAULT 1,
	PRIMARY KEY("id_metodo_pago" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "MOVIMIENTO_STOCK" (
	"id_movimiento"	INTEGER,
	"id_producto"	INTEGER NOT NULL,
	"tipo_movimiento"	TEXT(20) NOT NULL,
	"cantidad"	INTEGER NOT NULL,
	"stock_anterior"	INTEGER NOT NULL,
	"stock_nuevo"	INTEGER NOT NULL,
	"motivo"	TEXT,
	"id_usuario"	INTEGER NOT NULL,
	"fecha_movimiento"	TEXT DEFAULT (datetime('now', 'localtime')),
	PRIMARY KEY("id_movimiento" AUTOINCREMENT),
	FOREIGN KEY("id_producto") REFERENCES "PRODUCTO"("id_producto"),
	FOREIGN KEY("id_usuario") REFERENCES "USUARIO"("id_usuario")
);
CREATE TABLE IF NOT EXISTS "PLANES" (
	"id_plan"	INTEGER,
	"plan_nombre"	text(50),
	"precio"	NUMERIC,
	PRIMARY KEY("id_plan" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "PRODUCTO" (
	"id_producto"	INTEGER,
	"codigo"	TEXT(20) NOT NULL UNIQUE,
	"nombre"	TEXT(100) NOT NULL,
	"descripcion"	TEXT,
	"id_categoria"	INTEGER NOT NULL,
	"precio_compra"	NUMERIC(10, 2) NOT NULL,
	"precio_venta"	NUMERIC(10, 2) NOT NULL,
	"stock"	INTEGER DEFAULT 0,
	"stock_minimo"	INTEGER DEFAULT 5,
	"estado"	INTEGER DEFAULT 1,
	"fecha_registro"	TEXT DEFAULT (datetime('now', 'localtime')),
	PRIMARY KEY("id_producto" AUTOINCREMENT),
	FOREIGN KEY("id_categoria") REFERENCES "CATEGORIA_PRODUCTO"("id_categoria")
);
CREATE TABLE IF NOT EXISTS "ROL" (
	"id_rol"	INTEGER,
	"rol_nombre"	text(20),
	PRIMARY KEY("id_rol" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "USUARIO" (
	"id_usuario"	INTEGER,
	"nombre_usuario"	TEXT,
	"clave"	TEXT,
	"id_rol"	int,
	PRIMARY KEY("id_usuario" AUTOINCREMENT),
	FOREIGN KEY("id_rol") REFERENCES "ROL"("id_rol")
);
CREATE TABLE IF NOT EXISTS "VENTA" (
	"id_venta"	INTEGER,
	"numero_venta"	TEXT(20) NOT NULL UNIQUE,
	"id_cliente"	INTEGER,
	"id_usuario"	INTEGER NOT NULL,
	"fecha_venta"	TEXT DEFAULT (datetime('now', 'localtime')),
	"subtotal"	NUMERIC(10, 2) NOT NULL,
	"descuento"	NUMERIC(10, 2) DEFAULT 0,
	"total"	NUMERIC(10, 2) NOT NULL,
	"metodo_pago"	TEXT(20) NOT NULL,
	"estado"	TEXT(20) DEFAULT 'Completada',
	"nota_adicional"	TEXT,
	PRIMARY KEY("id_venta" AUTOINCREMENT),
	FOREIGN KEY("id_cliente") REFERENCES "CLIENTE"("id_cliente"),
	FOREIGN KEY("id_usuario") REFERENCES "USUARIO"("id_usuario")
);
CREATE TABLE IF NOT EXISTS "VLD" (
	"ID"	INTEGER,
	"DESC"	TEXT,
	PRIMARY KEY("ID" AUTOINCREMENT)
);
CREATE TABLE IF NOT EXISTS "pagos" (
	"id_pago"	INTEGER,
	"monto"	NUMERIC(10, 2),
	"id_plan"	INTEGER,
	"id_cliente"	int,
	"fecha_registro"	TEXT DEFAULT (datetime('now', 'localtime')),
	"nota_adicional"	TEXT,
	"estado"	INTEGER DEFAULT 1,
	PRIMARY KEY("id_pago" AUTOINCREMENT),
	FOREIGN KEY("id_cliente") REFERENCES "CLIENTE"("id_cliente"),
	FOREIGN KEY("id_plan") REFERENCES "PLANES"("id_plan")
);
CREATE TABLE IF NOT EXISTS "GASTOS" (
	"id_gasto"	INTEGER,
	"descripcion"	TEXT NOT NULL,
	"monto"	NUMERIC(10, 2) NOT NULL,
	"categoria"	TEXT,
	"fecha_registro"	TEXT DEFAULT (datetime('now', 'localtime')),
	"estado"	INTEGER DEFAULT 1,
	PRIMARY KEY("id_gasto" AUTOINCREMENT)
);
CREATE INDEX IF NOT EXISTS "idx_detalle_venta" ON "DETALLE_VENTA" (
	"id_venta"
);
CREATE INDEX IF NOT EXISTS "idx_movimiento_producto" ON "MOVIMIENTO_STOCK" (
	"id_producto"
);
CREATE INDEX IF NOT EXISTS "idx_producto_categoria" ON "PRODUCTO" (
	"id_categoria"
);
CREATE INDEX IF NOT EXISTS "idx_producto_codigo" ON "PRODUCTO" (
	"codigo"
);
CREATE INDEX IF NOT EXISTS "idx_venta_cliente" ON "VENTA" (
	"id_cliente"
);
CREATE INDEX IF NOT EXISTS "idx_venta_fecha" ON "VENTA" (
	"fecha_venta"
);
CREATE INDEX IF NOT EXISTS "idx_cliente_documento" ON "CLIENTE" (
    "documento"
);
CREATE INDEX IF NOT EXISTS "idx_cliente_nombre_completo" ON "CLIENTE" (
    "nombre",
    "apellido"
);
CREATE INDEX IF NOT EXISTS "idx_cliente_estado" ON "CLIENTE" (
    "estado"
);
CREATE INDEX IF NOT EXISTS "idx_pagos_cliente" ON "pagos" (
    "id_cliente"
);
CREATE INDEX IF NOT EXISTS "idx_pagos_fecha" ON "pagos" (
    "fecha_registro"
);
CREATE INDEX IF NOT EXISTS "idx_pagos_estado" ON "pagos" (
    "estado"
);
CREATE INDEX IF NOT EXISTS "idx_gastos_fecha" ON "GASTOS" (
    "fecha_registro"
);
CREATE INDEX IF NOT EXISTS "idx_gastos_estado" ON "GASTOS" (
    "estado"
);
CREATE TABLE IF NOT EXISTS "CONFIGURACION_NOTIFICACIONES" (
	"id" INTEGER PRIMARY KEY AUTOINCREMENT,
	"smtp_host" TEXT,
	"smtp_port" INTEGER,
	"smtp_user" TEXT,
	"smtp_password" TEXT,
	"smtp_ssl" INTEGER DEFAULT 1,
	"wa_api_url" TEXT,
	"wa_instance" TEXT,
	"wa_token" TEXT,
	"mensaje_template" TEXT,
	"email_activo" INTEGER DEFAULT 0,
	"wa_activo" INTEGER DEFAULT 0
);
CREATE TABLE IF NOT EXISTS "NOTIFICACIONES_HISTORIAL" (
	"id_notificacion" INTEGER PRIMARY KEY AUTOINCREMENT,
	"id_cliente" INTEGER,
	"fecha_vencimiento_aviso" TEXT,
	"fecha_envio" TEXT DEFAULT (datetime('now', 'localtime')),
	"medio" TEXT,
	"estado" INTEGER DEFAULT 1,
	FOREIGN KEY("id_cliente") REFERENCES "CLIENTE"("id_cliente")
);
COMMIT;
