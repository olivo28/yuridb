﻿using DotEnv = DotEnvFile.DotEnvFile;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;

namespace YuriDb.Core
{
    public enum MangaSitio : byte
    {
        Ninguno = 0,
        Interno = 1 << 0,
        Tmo = 1 << 1,
        Todos = Interno | Tmo
    }

    public enum MangaEstado : byte
    {
        Ninguno = 0,
        Activo = 1 << 0,
        Finalizado = 1 << 1,
        Abandonado = 1 << 2,
        Todos = Activo | Finalizado | Abandonado
    }

    public enum MangaTipo : byte
    {
        Ninguno = 0,
        Manga   = 1 << 0,
        Manhua  = 1 << 1,
        Manhwa  = 1 << 2,
        Novela  = 1 << 3,
        Propio  = 1 << 4,
        Otro    = 1 << 5,
        Todos = Manga | Manhua | Manhwa | Novela | Propio | Otro
    }

    public enum StaffTipo : byte
    {
        Ninguno = 0,
        Autor   = 1 << 0,
        Artista = 1 << 1,
        Todos   = Autor | Artista
    }

    public class StaffManga
    {
        public uint? Id { get; private set; }
        public uint? TmoId { get; private set; }
        public string Nombre { get; set; }
        public StaffTipo Tipo { get; set; }
        
        public StaffManga(uint? id, uint? tmoId)
        {
            Id = id;
            TmoId = tmoId;
        }

        public StaffManga(uint? tmoId) :
         this(null, tmoId)
        {

        }

        public StaffManga():
          this(null, null)
        {

        }
    }
        
    public class Revista
    {
        public uint? Id { get; private set; }
        public uint? TmoId { get; private set; }
        public string Nombre { get; set; }
        public TimeSpan? Periodicidad { get; set; }

        public Revista(uint? id, uint? tmoId)
        {
            Id = id;
            TmoId = tmoId;
        }

        public Revista(uint? tmoId) : 
            this(null, tmoId)
        {

        }

        public Revista() :
         this(null)
        { 

        }

    }

    public class MangaNomAlterno
    {
        public uint? Id { get; private set; }
        public string Nombre { get; set; }
      
        public MangaNomAlterno(uint? id)
        {
            Id = id;
        }

        public MangaNomAlterno() :
            this(null)
        {

        }
    }

    public class Capítulos
    {
        public uint? Id { get; private set; }
        public uint? Tomo { get; private set; }
        public uint? MangaId { get; private set; }
        public float Capítulo { get; set; }
        public string Nombre { get; set; }
        public uint? OwnerId { get; private set; }
        public DateTime? Creación { get; set; }

        public Capítulos(uint? id, uint? tomo)
        {
            Id = id;
            Tomo = tomo;
        }

        public Capítulos(uint? tomo) :
               this(null, tomo)
        {

        }
        public Capítulos() :
               this(null)
        {

        }

    }

    public class Scans
    {
        public uint? Id { get; private set; }
        public uint? TmoId { get; private set; }
        public string Nombre { get; set; }
        public Uri Logo { get; set; }
        public string Website { get; set; }
        
        public Scans(uint? id, uint? tmoId)
        {
            Id = id;
            TmoId = tmoId;
        }

        public Scans(uint? tmoId) :
            this(null, tmoId)
        {

        }
        public Scans() :
            this(null)
        {

        }

    }

    public class Joints
    {
        public uint? Id { get; private set; }
        public uint? CapítuloId { get; private set; }      
        public uint? ScanId { get; private set; }

        public Joints(uint? id)
        {
            Id = id;
        }
        public Joints() :
            this(null)
        {

        }
    }


    
    public class MangaYuri
    {
        public DateTime? TmoCreacion { get; private set; }
        public MangaEstado Estado { get; set; }
        public MangaTipo Tipo { get; set; }
        public string Descripcion { get; set; }
        public string Nombre { get; set; }
        public uint Capitulos { get; set; }
        public uint? Id { get; private set; }
        public uint? TmoId { get; private set; }
        public Uri Imagen { get; set; }
        public List<StaffManga> Staff { get; set; }
        public List<MangaNomAlterno> NombresAlternos { get; set; }
        public Revista Revista { get; set; }

        public MangaYuri(uint? id, uint? tmoId, DateTime? tmoCreacion)
        {
            Id = id;
            TmoId = tmoId;
            TmoCreacion = tmoCreacion;
            Capitulos = 0;
            Imagen = null;
            Staff = new List<StaffManga>();
            NombresAlternos = new List<MangaNomAlterno>();
        }

        public MangaYuri(uint? tmoId, DateTime? tmoCreacion) : 
            this(null, tmoId, tmoCreacion)
        {
        }

        public MangaYuri():
            this(null, null, null)
        {

        }

        public Uri GenerarLink()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("https://www.tumangaonline.com/biblioteca/");
            sb.Append(Tipo.ToString().ToLower());
            sb.Append("s/");
            sb.Append(TmoId);
            sb.Append("/");
            sb.Append(Uri.EscapeDataString(Nombre
                .Trim()
                .Replace("-", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace('á', 'a')
                .Replace('é', 'e')
                .Replace('í', 'i')
                .Replace('ó', 'o')
                .Replace('ú', 'u')
                .Replace(' ', '-')
                .Replace("¿", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace("¡", "")
                .Replace(".", "")
                .Replace(",", "")
                .Replace(";", "")
                .Replace(":", "")
            ));
            return new Uri(sb.ToString());
        }

    }

    public class YuriDb
    {
        private volatile bool _disposed;
        public bool Disposed 
        {
            get
            {
                return _disposed;
            }
        }
        private volatile bool _connected;
        public bool Connected { 
            get 
            {
                return _connected;   
            }
        } 
        public string Password { get; private set; }
        public string Host { get; private set; }
        public string User { get; private set; }
        public string Db   { get; private set; }
        public uint Port    { get; private set; }
        private MySqlConnection _connection;
        private static object _lock = new object();

        public YuriDb(string user, string password, string db, string host, uint port)
        {
            User = user;
            Password = password;
            Db = db;
            Host = host;
            Port = port;
            _connection = new MySqlConnection($"SERVER={Host};PORT={Port};UID={User};PASSWORD={Password};CHARACTERSET=utf8mb4;SSLMODE=none");
        }


        public void CreateConnection()
        {
            lock (_connection) {
                if (_connection == null || _disposed) {
                    _connection = new MySqlConnection($"SERVER={Host};PORT={Port};UID={User};PASSWORD={Password};CHARACTERSET=utf8mb4;SSLMODE=none");
                } 
            }
        }

        public void OpenConnection()
        {
            lock (_connection) {
                if (!_connected && !_disposed) {   
                    _connection.Open();
                    var cmd = _connection.CreateCommand();
                    cmd.CommandText = "SET NAMES utf8mb4";
                    cmd.ExecuteNonQuery();
                    _connected = true;
                }
            }
        }

        public void CloseConnection()
        {
            lock (_connection) {
                if(_connected && !_disposed) {
                    _connection.Close();
                    _connected = false;
                }
            }
        }

        public void DisposeConnection()
        {
            lock (_connection) {
                if (_connection != null && !_disposed) {
                    _connection.Dispose();
                    _disposed = true;
                }
            }
            
        }

        public void CrearBaseDeDatos()
        {   lock (_connection){
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{Db}` CHARACTER SET = 'utf8mb4'";
                cmd.ExecuteNonQuery();
            }
        }

        public void CrearTablas()
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = 
$@"CREATE TABLE IF NOT EXISTS `{Db}`.`mangas` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoId           INT UNSIGNED        NULL UNIQUE KEY,
tmoCreación     DATE                NULL COMMENT 'Se usa internamente para búsqueda binaria',
nombre          VARCHAR(249)        NOT NULL,
imagen          VARCHAR(249)        NULL,
descripción     VARCHAR(1024)       NOT NULL DEFAULT '',
capítulos       INT UNSIGNED        NOT NULL DEFAULT 0,
estado          TINYINT UNSIGNED    NOT NULL,
tipo            TINYINT UNSIGNED    NOT NULL,
revista         INT UNSIGNED        NULL,
UNIQUE (`nombre`, `tipo`),
FULLTEXT INDEX (`nombre`),
CONSTRAINT CHECK((tmoId IS NULL AND tmoCreación IS NULL) OR 
                 (tmoID IS NOT NULL AND tmoCreación IS NOT NULL))
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de los mangas extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`revistas` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoId           INT UNSIGNED        NULL UNIQUE KEY,
nombre          VARCHAR(249)        NOT NULL UNIQUE KEY,
periodicidad    TIME              NULL,
FULLTEXT INDEX(`nombre`)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las revistas extraídas de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`staffs` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoId           INT UNSIGNED        NULL, 
nombre          VARCHAR(247)        NOT NULL,
mangaId         INT UNSIGNED        NOT NULL,
tipo            TINYINT UNSIGNED    NOT NULL,
UNIQUE (`nombre`, `mangaId`, `tipo`, `tmoId`),
FULLTEXT INDEX (`nombre`)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las autores y artistas extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`nombres_alternos` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
nombre          VARCHAR(249)        NOT NULL,
mangaId         INT UNSIGNED        NOT NULL,
FULLTEXT INDEX (`nombre`)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de nombres alternos de los mangas extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`capítulos` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tomo            TINYINT UNSIGNED    NULL,
mangaId         INT UNSIGNED        NOT NULL,
capítulo        INT UNSIGNED        NOT NULL,
nombre          VARCHAR(249)        NOT NULL DEFAULT '',
ownerId         INT UNSIGNED        NOT NULL,
creación        DATE                NULL,
UNIQUE (`tomo`, `mangaId`, `capítulo`, `ownerId`)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de los ultimos capítulos estrenados en TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`scans` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoId           INT UNSIGNED        NULL,
nombre          VARCHAR(249)        NOT NULL UNIQUE KEY,
logo            VARCHAR(249)        NULL,
website         VARCHAR(249)        NOT NULL
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las scans extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`joints` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
capítuloId      INT UNSIGNED        NOT NULL,
scanId          INT UNSIGNED        NOT NULL,
UNIQUE (`capítuloId`, `scanId`)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las joints extraídos de TMO'
CREATE TABLE IF NOT EXISTS `{Db}`.`nombres_capítulos` (
capítuloId      INT UNSIGNED        NOT NULL PRIMARY KEY,
nombre          VARCHAR(249)        NULL,
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las nombres de los capítulos extraídos de TMO'";

                cmd.ExecuteNonQuery();
            }
        }

        public uint AgregarCapítulo(Capítulos capítulo)
        {
            uint id = 0;
            lock(_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`capítulos` 
( tomo, mangaId, capítulo, nombre, ownerId, creación
) values (
  @tomo, @mangaId, @capítulo, @nombre, @ownerId, @creación
);
SELECT LAST_INSERT_ID()";
                cmd.Parameters.AddWithValue("@tomo", capítulo.Tomo);
                cmd.Parameters.AddWithValue("@mangaId", capítulo.MangaId);
                cmd.Parameters.AddWithValue("@capítulo", capítulo.Capítulo);
                cmd.Parameters.AddWithValue("@nombre", capítulo.Nombre);
                cmd.Parameters.AddWithValue("@ownerId", capítulo.OwnerId);
                cmd.Parameters.AddWithValue("@creación", capítulo.Creación);
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            return id;
        }

        public uint AgregarScan(Scans scans)
        {
            uint id = 0;
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`scans` 
( tmoId, nombre, logo, website
) values (
  @tmoId, @nombre, @logo, @website
);
SELECT LAST_INSERT_ID()";
                cmd.Parameters.AddWithValue("@tmoId", scans.TmoId);
                cmd.Parameters.AddWithValue("@nombre", scans.Nombre);
                cmd.Parameters.AddWithValue("@logo", scans.Logo);
                cmd.Parameters.AddWithValue("@website", scans.Website);
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            return id;
        }

        public uint AgregarNombresCapítulos()
        {
            uint id = 0;
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`nombres_capítulos` 
( capítuloId, nombre
) values (
  @capítuloId, @nombre
);
SELECT LAST_INSERT_ID()";
                cmd.Parameters.AddWithValue("@capítuloId", .);
                cmd.Parameters.AddWithValue("@nombre", .);
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            return id;
        }

        public uint AgregarJoints(Joints joints)
        {
            uint id = 0;
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`joints` 
( capítuloId, scanId  
) values (
  @capítuloId, @scanId
);
SELECT LAST_INSERT_ID()";
                cmd.Parameters.AddWithValue("@capítuloId", joints.CapítuloId);
                cmd.Parameters.AddWithValue("@scanId", joints.ScanId);
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            return id;
        }

        public uint AgregarRevista(Revista revista)
        {
            uint id = 0;
            lock (_connection)
                {
                    MySqlCommand cmd = _connection.CreateCommand();
                    string periodicidad = revista.Periodicidad == null ? null : revista.Periodicidad.Value.ToString("hhh:mm:ss");
                    cmd.CommandText =
    $@"INSERT INTO `{Db}`.`revistas` 
( tmoId, nombre, periodicidad
) values (
  @tmoId, @nombre, @periodicidad
);
SELECT LAST_INSERT_ID()";
                    cmd.Parameters.AddWithValue("@tmoId", revista.TmoId);
                    cmd.Parameters.AddWithValue("@nombre", revista.Nombre);
                    cmd.Parameters.AddWithValue("@periodicidad", periodicidad);
                    cmd.Prepare();
                    id = Convert.ToUInt32(cmd.ExecuteScalar());
                }
            return id;
        }

        public uint AgregarStaffManga(StaffManga staff, uint mangaId)
        {
            uint id = 0;
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`staffs` 
( nombre, tmoId, mangaId, tipo
) values (
  @nombre, @tmoId, @mangaId, @tipo
)";
                cmd.Parameters.AddWithValue("@nombre", staff.Nombre);
                cmd.Parameters.AddWithValue("@tmoId", staff.TmoId);
                cmd.Parameters.AddWithValue("@mangaId", mangaId);
                cmd.Parameters.AddWithValue("@tipo", staff.Tipo);
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            return id;
        }

        public uint AgregarNomAltManga(MangaNomAlterno mangaNomAlterno, uint mangaId)
        {
            uint id = 0;
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`nombres_alternos` 
( nombre, mangaId
) values (
  @nombre, @mangaid
);
SELECT LAST_INSERT_ID()";
                cmd.Parameters.AddWithValue("@nombre", mangaNomAlterno.Nombre);
                cmd.Parameters.AddWithValue("@mangaid", mangaId);
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            return id;
        }

        public uint AgregarManga(MangaYuri manga)
        {
            if (manga == null) {
                throw new ArgumentNullException("manga es nulo");
            }

            if (manga.Id != null) {
                throw new ArgumentException("id debe ser null");
            }

            if (manga.Nombre == null || manga.Nombre.Length == 0) {
                throw new ArgumentException("el nombre es nulo o vacío");
            }

            if (manga.Descripcion == null) {
                throw new ArgumentException("la descripción no puede ser nula");
            }

            string tmoc = manga.TmoCreacion.Value == null ? null : manga.TmoCreacion.Value.Date.ToString("yyyy-MM-dd");
            uint id = 0;

            uint revId = 0;
            
            if (manga.Revista != null) {
                try {
                    revId = AgregarRevista(manga.Revista);
                } catch(MySqlException e) {
                    if (e.Number != (int) MySqlErrorCode.DuplicateKeyEntry) {
                            throw e;
                    }
                }
            }

            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = 
$@"INSERT INTO `{Db}`.`mangas` 
(tmoId, tmoCreación, nombre, 
 descripción, capítulos, imagen, 
 tipo, estado, revista
) values (
  @tmoId, @tmoCreación, @nombre, 
  @descripción, @capítulos, @imagen, 
  @tipo, @estado, @revista
);
SELECT LAST_INSERT_ID()";
                cmd.Parameters.AddWithValue("@tmoId", manga.TmoId);
                cmd.Parameters.AddWithValue("@tmoCreación", tmoc);
                cmd.Parameters.AddWithValue("@nombre", manga.Nombre);
                cmd.Parameters.AddWithValue("@descripción", manga.Descripcion);
                cmd.Parameters.AddWithValue("@capítulos", manga.Capitulos);
                cmd.Parameters.AddWithValue("@imagen", manga.Imagen);
                cmd.Parameters.AddWithValue("@tipo", manga.Tipo);
                cmd.Parameters.AddWithValue("@estado", manga.Estado);
                if (revId == 0) {
                    cmd.Parameters.AddWithValue("@revista", null);
                } else {
                    cmd.Parameters.AddWithValue("@revista", revId);
                }
                cmd.Prepare();
                id = Convert.ToUInt32(cmd.ExecuteScalar());
            }
            foreach (StaffManga staff in manga.Staff) {
                AgregarStaffManga(staff, id);
            }
            foreach (MangaNomAlterno alt in manga.NombresAlternos) {
                AgregarNomAltManga(alt, id);
            }
            return id;
        }

        public uint GetCantidadMangas(MangaSitio sitio = MangaSitio.Todos)
        {
            return GetCantidadMangas(MangaTipo.Todos, MangaEstado.Todos, sitio);
        }

        public uint GetCantidadMangas(MangaTipo tipo, MangaEstado estado, MangaSitio sitio = MangaSitio.Todos)
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                string ssitio = "AND (";
                if (((byte) sitio & (byte) MangaSitio.Tmo) > 0) {
                    ssitio += "(`tmoId` IS NOT NULL AND `tmoCreación` IS NOT NULL)";
                } else {
                    ssitio += "FALSE";
                }
                ssitio += " OR ";
                if(((byte) sitio & (byte) MangaSitio.Interno) > 0) {
                    ssitio += "(`tmoId` IS NULL AND `tmoCreación` IS NULL)";
                }  else {
                    ssitio += "FALSE";
                }
                ssitio += ")";
                cmd.CommandText = 
$@"SELECT COUNT(*) FROM `{Db}`.`mangas` 
    WHERE (`tipo` & {(byte) tipo} ) > 0 
        AND (`estado` & {(byte) estado}) > 0
        {ssitio}";
                return Convert.ToUInt32(cmd.ExecuteScalar());
            }            
        }

        public uint GetCantidadMangas(MangaEstado estado, MangaSitio sitio = MangaSitio.Todos)
        {
            return GetCantidadMangas(MangaTipo.Todos, estado, sitio);
        }

        public uint GetCantidadMangas(MangaTipo tipo, MangaSitio sitio = MangaSitio.Todos)
        {
            return GetCantidadMangas(tipo, MangaEstado.Todos, sitio);
        }

        public MangaYuri GetUltimaAgregacion(MangaSitio sitio = MangaSitio.Todos)
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{Db}`.`mangas` ORDER BY `id` DESC LIMIT 1";
                MySqlDataReader reader = cmd.ExecuteReader();
                MangaYuri[] result = GetMangas(reader);
                if (result.Length == 0) {
                    return null;
                } else {
                    return result[0];
                }
            }
        }

        private MangaYuri[] GetMangas(MySqlDataReader reader)
        {
            List<MangaYuri> lista = new List<MangaYuri>();
            while(reader.Read()) {
                lista.Add(GetManga(reader));
            }
            reader.Close();
            foreach (MangaYuri manga in lista) {
                manga.Staff = GetStaff(manga.Id.Value);
                manga.NombresAlternos = GetNombresAlternos(manga.Id.Value);                
                if (manga.Revista != null) {
                    manga.Revista = GetRevista(manga.Revista.Id.Value);
                }
            }
            return lista.ToArray();
        }

        private MangaYuri GetManga(MySqlDataReader reader)
        {          
            Uri imagen = Convert.IsDBNull(reader[4]) ? null : new Uri((string) reader[4]);
            MangaYuri manga = new MangaYuri(
                (UInt32?) reader[0],
                Convert.IsDBNull(reader[1]) ? null : (UInt32?) reader[1],
                Convert.IsDBNull(reader[2]) ? null : (DateTime?) reader[2]
            ) {
                Nombre = (string) reader[3],
                Imagen = imagen,
                Descripcion = (string) reader[5],
                Capitulos = (UInt32) reader[6],
                Estado = (MangaEstado) reader[7],
                Tipo = (MangaTipo) reader[8],
            };
            if (!Convert.IsDBNull(reader[9])) {
                manga.Revista = new Revista((UInt32?) reader[9], null);
            }
            return manga;            
        }

        public MangaYuri GetManga(string nombre)
        {
            MangaYuri[] result = GetMangas(nombre, 1);
            if (result.Length == 0) {
                return null;
            } else {
                return result[0];
            }
        }

        public MangaYuri GetManga(uint id)
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{Db}`.`mangas` WHERE `id` = {id} LIMIT 1";
                MySqlDataReader reader = cmd.ExecuteReader();
                MangaYuri[] result = GetMangas(reader);
                if (result.Length == 0) {
                    return null;
                } else {
                    return result[0];
                } 
            }           
        }

        public MangaYuri[] GetMangas(string nombre, uint limit)
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = 
$@"SELECT * FROM `{Db}`.`mangas` 
    WHERE (MATCH(`nombre`) AGAINST(@nombre)) OR 
          (`nombre` LIKE @like) 
    ORDER BY `nombre` ASC LIMIT {limit}";
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@like", '%' + nombre + '%');
                cmd.Prepare();
                MySqlDataReader reader = cmd.ExecuteReader();
                return GetMangas(reader);
            }
        }

        public MangaYuri[] GetMangasStaff(string nombre, uint limit)
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"SELECT mangas.*  FROM `{Db}`.`mangas` INNER JOIN 
    `{Db}`.`staffs` ON (mangas.id = staffs.mangaId AND MATCH(staffs.Nombre) 
    AGAINST(@staff)) GROUP BY mangas.id ORDER BY mangas.id ASC LIMIT {limit}"; 
                cmd.Parameters.AddWithValue("@staff", nombre);
                cmd.Prepare();
                MySqlDataReader reader = cmd.ExecuteReader();
                return GetMangas(reader);
            }
        }

        public MangaYuri[] GetMangasStaff(string nombre)
        {
            return GetMangasStaff(nombre, 10);
        }


        public MangaYuri[] GetMangas(string nombre)
        {
            return GetMangas(nombre, 10);
        }

        public MangaYuri GetMangaRandom()
        {
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{Db}`.`mangas` ORDER BY RAND() LIMIT 1";
                MySqlDataReader reader = cmd.ExecuteReader();
                MangaYuri[] result = GetMangas(reader);
                if (result.Length == 0) {
                    return null;
                }  else {
                    return result[0];
                }
            }
        }

        public List<StaffManga> GetStaff(uint mangaId)
        {
            lock(_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{Db}`.`staffs` WHERE `mangaId` = {mangaId}";
                MySqlDataReader reader = cmd.ExecuteReader();
                var staffs = new List<StaffManga>();
                while (reader.Read()) {
                    staffs.Add(GetStaff(reader));
                }
                reader.Close();
                return staffs;
            }
        }

        private StaffManga GetStaff(MySqlDataReader reader)
        {
            StaffManga staff = new StaffManga(
                (UInt32?) reader[0], 
                Convert.IsDBNull(reader[1]) ? null : (UInt32?) reader[1]
            );
            staff.Nombre = (string) reader[2];
            staff.Tipo = (StaffTipo) reader[4];
            return staff;
        }

        public List<MangaNomAlterno> GetNombresAlternos(uint mangaId)
        {
            lock(_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{Db}`.`nombres_alternos` WHERE `mangaId` = {mangaId}";
                MySqlDataReader reader = cmd.ExecuteReader();
                var alternos = new List<MangaNomAlterno>();
                while (reader.Read()) {
                    alternos.Add(GetNombreAlterno(reader));
                }
                reader.Close();
                return alternos;
            }
        }

        private MangaNomAlterno GetNombreAlterno(MySqlDataReader reader)
        {
            var nom = new MangaNomAlterno((UInt32?) reader[0]);
            nom.Nombre = (string) reader[1];
            return nom;
        }

        public Revista GetRevista(uint revistaId) 
        {
            lock(_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = $"SELECT * FROM `{Db}`.`revistas` WHERE `id` = {revistaId}";
                MySqlDataReader reader = cmd.ExecuteReader();
                Revista revista = null;
                if (reader.Read()) {
                    revista = GetRevista(reader);
                }
                reader.Close();
                return revista;
            }
        }

        private Revista GetRevista(MySqlDataReader reader)
        {
            var revista = new Revista((UInt32?) reader[0], (UInt32?) reader[1]);
            revista.Nombre = (string) reader[2];
            revista.Periodicidad = Convert.IsDBNull(reader[3]) ? null : (TimeSpan?) reader[3];
            return revista;
        }

        private static void DotEnvSetup()
        {
            var env = Environment.GetEnvironmentVariables();
            if (!env.Contains("YURIDB_HOST")) {
                try {
                    Dictionary<string, string> vars = DotEnv.LoadFile(".env");
                    DotEnv.InjectIntoEnvironment(vars);
                }
                catch (Exception) {
                    Environment.Exit(-1);
                }
            }
        }


        private static YuriDb _instancia;

        public static YuriDb Instancia
        {
            get 
            {
                lock(_lock) {
                    if (_instancia == null) {
                        DotEnvSetup();
                        uint port = 0;
                        
                        try {
                            port = UInt32.Parse(ValidarExistenciaEnv("YURIDB_PORT"));
                        } catch (FormatException) {
                            Console.WriteLine("[Error] La variable de entorno YURIDB_PORT contiene un puerto inválido");
                            Environment.Exit(-1);
                        }

                        YuriDb ydb = new YuriDb (
                            ValidarExistenciaEnv("YURIDB_USER"),
                            ValidarExistenciaEnv("YURIDB_PASSWORD"),
                            ValidarExistenciaEnv("YURIDB_DB"),
                            ValidarExistenciaEnv("YURIDB_HOST"),
                            port
                        );
                        _instancia = ydb;
                    }
                    return _instancia;
                }
            }
        }

        public static string ValidarExistenciaEnv(string variable) {
            string result = Environment.GetEnvironmentVariable(variable);
            if (result == null) {
                Console.WriteLine("[Error] La variable de entorno {0} no se ha establecido", variable);
                Environment.Exit(-1);
            }
            return result;
        }

    }
}
