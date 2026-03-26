-- DUMMY DATA FOR TESTING
BEGIN TRANSACTION;

-- Inserción de 20 Clientes Muestra (Usamos IDs 9001 al 9020 para no chocar con tus datos reales)
INSERT INTO CLIENTE (id_cliente, documento, nombre, apellido, fecha_nacimiento, id_genero, direccion, telefono, email, id_plan, fecha_inicio, fecha_vencimiento, fecha_registro, estado) VALUES 
(9001, '35111222', 'Martín', 'Palermo', '1980-05-14', 1, 'Calle 1', '11223344', 'martin@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-5 days', '-12 hours'), 1),
(9002, '36222333', 'Lucía', 'Fernández', '1992-08-21', 2, 'Calle 2', '22334455', 'lucia@mail.com', 1, date('now'), date('now', '+1 day'), date('now', '-2 days'), 1),
(9003, '37333444', 'Diego', 'Pérez', '1960-10-30', 1, 'Calle 3', '33445566', 'diego@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-10 days'), 1),
(9004, '38444555', 'Valeria', 'Mazza', '1972-02-17', 2, 'Calle 4', '44556677', 'valeria@mail.com', 2, date('now'), date('now', '+7 days'), date('now', '-1 days'), 1),
(9005, '39555666', 'Lionel', 'Gómez', '1987-06-24', 1, 'Calle 5', '55667788', 'lionel@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-15 days'), 1),
(9006, '40666777', 'Antonela', 'Roccuzzo', '1988-02-26', 2, 'Calle 6', '66778899', 'anto@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-15 days', '-4 hours'), 1),
(9007, '41777888', 'Emanuel', 'Ginóbili', '1977-07-28', 1, 'Calle 7', '77889900', 'manu@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-3 days'), 1),
(9008, '42888999', 'Gabriela', 'Sabatini', '1970-05-16', 2, 'Calle 8', '88990011', 'gaby@mail.com', 1, date('now'), date('now', '+1 day'), date('now', '-4 days'), 1),
(9009, '43999000', 'Juan', 'Martín', '1988-09-23', 1, 'Calle 9', '99001122', 'juan@mail.com', 2, date('now'), date('now', '+7 days'), date('now', '-6 days'), 1),
(9010, '44000111', 'Milena', 'Espósito', '1991-10-10', 2, 'Calle 10', '00112233', 'milena@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-8 days'), 1),
(9011, '45112233', 'Ricardo', 'Darín', '1957-01-16', 1, 'Calle 11', '11223300', 'ricardo@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-11 days'), 1),
(9012, '46223344', 'Susana', 'Giménez', '1944-01-29', 2, 'Calle 12', '22334411', 'susana@mail.com', 1, date('now'), date('now', '+1 day'), date('now', '-12 days'), 1),
(9013, '47334455', 'Guillermo', 'Francella', '1955-02-14', 1, 'Calle 13', '33445522', 'guille@mail.com', 2, date('now'), date('now', '+7 days'), date('now', '-13 days'), 1),
(9014, '48445566', 'Moria', 'Casán', '1946-08-16', 2, 'Calle 14', '44556633', 'moria@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-14 days'), 1),
(9015, '49556677', 'Charly', 'García', '1951-10-23', 1, 'Calle 15', '55667744', 'charly@mail.com', 1, date('now'), date('now', '+1 day'), date('now', '-18 days'), 1),
(9016, '50667788', 'Tini', 'Stoessel', '1997-03-21', 2, 'Calle 16', '66778855', 'tini@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-20 days'), 1),
(9017, '51778899', 'Fito', 'Páez', '1963-03-13', 1, 'Calle 17', '77889966', 'fito@mail.com', 2, date('now'), date('now', '+7 days'), date('now', '-22 days'), 1),
(9018, '52889900', 'Natalia', 'Oreiro', '1977-05-19', 2, 'Calle 18', '88990077', 'nati@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-25 days'), 1),
(9019, '53990011', 'Andrés', 'Calamaro', '1961-08-22', 1, 'Calle 19', '99001188', 'andres@mail.com', 1, date('now'), date('now', '+1 day'), date('now', '-28 days'), 1),
(9020, '54001122', 'Mercedes', 'Sosa', '1935-07-09', 2, 'Calle 20', '00112299', 'mercedes@mail.com', 3, date('now'), date('now', '+30 days'), date('now', '-29 days'), 1);

-- Inserción de 20 Pagos correspondientes a las fechas en que se registraron
-- Asume la tabla de planes creada: 1 = $3000 (Clase), 2 = $10000 (Semana), 3 = $20000 (Mensual)
INSERT INTO pagos (monto, id_plan, id_cliente, fecha_registro) VALUES 
(20000, 3, 9001, date('now', '-5 days', '-12 hours')),
(3000,  1, 9002, date('now', '-2 days')),
(20000, 3, 9003, date('now', '-10 days')),
(10000, 2, 9004, date('now', '-1 days')),
(20000, 3, 9005, date('now', '-15 days')),
(20000, 3, 9006, date('now', '-15 days', '-4 hours')),
(20000, 3, 9007, date('now', '-3 days')),
(3000,  1, 9008, date('now', '-4 days')),
(10000, 2, 9009, date('now', '-6 days')),
(20000, 3, 9010, date('now', '-8 days')),
(20000, 3, 9011, date('now', '-11 days')),
(3000,  1, 9012, date('now', '-12 days')),
(10000, 2, 9013, date('now', '-13 days')),
(20000, 3, 9014, date('now', '-14 days')),
(3000,  1, 9015, date('now', '-18 days')),
(20000, 3, 9016, date('now', '-20 days')),
(10000, 2, 9017, date('now', '-22 days')),
(20000, 3, 9018, date('now', '-25 days')),
(3000,  1, 9019, date('now', '-28 days')),
(20000, 3, 9020, date('now', '-29 days'));

COMMIT;
