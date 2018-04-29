using DotEnv = DotEnvFile.DotEnvFile;
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
        Otro    = 1 << 3,
        Todo   = 1 << 6,
        Todos = Autor | Artista | Otro | Todo
    }

    public class StaffManga
    {
        public uint? Id { get; set; }
        public uint? Tmoid { get; set; }
        public string Nombre { get; set; }
        public uint? MangaId { get; set; }
        public StaffTipo Tipo { get; set; }
        
        public StaffManga(uint? id, uint? tmoId, uint? mangaId)
        {
            Id = id;
            Tmoid = tmoId;
            MangaId = mangaId;

        }

        public StaffManga(uint? id, uint? tmoId) :
         this(null, id, tmoId)
        { }
    }
        
    public class Revista
    {
        public uint? Id { get; set; }
        public uint? Tmoid { get; set; }
        public string Nombre { get; set; }
        public TimeSpan Periodicidad { get; set; }

        public Revista(uint? id, uint? tmoId)
        {
            Id = id;
            Tmoid = tmoId;
        }

        public Revista(uint? id) :
         this(null, id)
        { }

    }

    public class MangaNomAlterno
    {
        public uint? Id { get; set; }
        public string Nombre { get; set; }
        public uint? MangaId { get; set; }
      
        public MangaNomAlterno(uint? id, uint? mangaId)
        {
            Id = id;
            MangaId = mangaId;
        }

        public MangaNomAlterno(uint? id) :
            this(null, id)
        { }
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
        public string Autor { get; set; }
        public string Artista { get; set; }
        public string Revista    { get; set; }

        public MangaYuri(uint? id, uint? tmoId, DateTime? tmoCreacion)
        {
            Id = id;
            TmoId = tmoId;
            TmoCreacion = tmoCreacion;
            Capitulos = 0;
            Imagen = null;
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
            _connection = new MySqlConnection($"SERVER={Host};PORT={Port};UID={User};PASSWORD={Password}");
        }


        public void CreateConnection()
        {
            lock (_connection) {
                if (_connection == null || _disposed) {
                    _connection = new MySqlConnection($"SERVER={Host};PORT={Port};UID={User};PASSWORD={Password}");
                } 
            }
        }

        public void OpenConnection()
        {
            lock (_connection) {
                if (!_connected && !_disposed) {   
                    _connection.Open();
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
                cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{Db}` CHARACTER SET = 'utf8'";
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
nombre          VARCHAR(512)        NOT NULL,
imagen          VARCHAR(256)        NULL,
descripción     VARCHAR(1024)       NOT NULL DEFAULT '',
capítulos       INT UNSIGNED        NOT NULL DEFAULT 0,
estado          TINYINT UNSIGNED    NOT NULL,
tipo            TINYINT UNSIGNED    NOT NULL,
FULLTEXT INDEX (nombre),
CONSTRAINT CHECK((tmoId IS NULL AND tmoCreación IS NULL) OR 
                 (tmoID IS NOT NULL AND tmoCreación IS NOT NULL))
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de los mangas extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`revistas` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoid           INT UNSIGNED        NULL UNIQUE KEY,
nombre          VARCHAR(256)        NULL,
periodicidad    TIMESTAMP           NULL
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las revistas extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{Db}`.`staffs` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoid           INT UNSIGNED        NULL UNIQUE KEY, 
nombre          VARCHAR(256)        NULL,
mangaid         INT UNSIGNED        NOT NULL,
tipo            INT UNSIGNED        NOT NULL,
UNIQUE (nombre, mangaid)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las autores y artistas extraídos de TMO';
CREATE TABLE IF NOT EXISTS `{ Db}`.`nombresAlternos` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
nombre         VARCHAR(256)       NULL,
mangaid         INT UNSIGNED        NOT NULL
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de las nombres alternos de los mangas extraídos de TMO'";
                cmd.ExecuteNonQuery();
            }
        }

        public void AgregarRevista(Revista revista)
        {
            lock (_connection)
                {
                    MySqlCommand cmd = _connection.CreateCommand();
                    cmd.CommandText =
    $@"INSERT INTO `{Db}`.`revista` 
( tmoid, nombre, periodicidad
) values (
  @tmoId, @nombre, @periodicidad
)";
                    cmd.Parameters.AddWithValue("@tmoid", revista.Tmoid);
                    cmd.Parameters.AddWithValue("@nombre", revista.Nombre);
                    cmd.Parameters.AddWithValue("@periodicidad", revista.Periodicidad);
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }

        }

        public void AgregarStaffM(StaffManga staff)
        {
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`staffs` 
( nombre, tmoid, mangaid, tipo
) values (
  @nombre, @tmoid, @mangaid, @tipo
)";
                cmd.Parameters.AddWithValue("@nombre", staff.Nombre);
                cmd.Parameters.AddWithValue("@tmoid", staff.Tmoid);
                cmd.Parameters.AddWithValue("@mangaid", staff.MangaId);
                cmd.Parameters.AddWithValue("@tipo", staff.Tipo);
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }

        }

        public void AgregarNomAltManga(MangaNomAlterno mangaNomAlterno)
        {
            lock (_connection)
            {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText =
$@"INSERT INTO `{Db}`.`nombresAlternos` 
( nombres, mangaid
) values (
  @nombres, @mangaid
)";
                cmd.Parameters.AddWithValue("@nombres", mangaNomAlterno.Nombre);
                cmd.Parameters.AddWithValue("@mangaid", mangaNomAlterno.MangaId);
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }

        }

        public void AgregarManga(MangaYuri manga)
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
            lock (_connection) {
                MySqlCommand cmd = _connection.CreateCommand();
                cmd.CommandText = 
$@"INSERT INTO `{Db}`.`mangas` 
( tmoId, tmoCreación, nombre, 
  descripción, capítulos, imagen, tipo, estado
) values (
    @tmoId, @tmoCreación, @nombre, 
    @descripción, @capítulos, @imagen, @tipo, @estado
)";
                cmd.Parameters.AddWithValue("@tmoId", manga.TmoId);
                cmd.Parameters.AddWithValue("@tmoCreación", tmoc);
                cmd.Parameters.AddWithValue("@nombre", manga.Nombre);
                cmd.Parameters.AddWithValue("@descripción", manga.Descripcion);
                cmd.Parameters.AddWithValue("@capítulos", manga.Capitulos);
                cmd.Parameters.AddWithValue("@imagen", manga.Imagen);
                cmd.Parameters.AddWithValue("@tipo", manga.Tipo);
                cmd.Parameters.AddWithValue("@estado", manga.Estado);
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
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
            return lista.ToArray();
        }

        private MangaYuri GetManga(MySqlDataReader reader)
        {          
            var imagen = (string) reader[4];
            MangaYuri manga = new MangaYuri(
                (UInt32?) reader[0],
                (UInt32?) reader[1],
                (DateTime?) reader[2]
            ) {
                Nombre = (string) reader[3],
                Imagen = imagen == null ? null : new Uri(imagen),
                Descripcion = (string) reader[5],
                Capitulos = (UInt32) reader[6],
                Estado = (MangaEstado) reader[7],
                Tipo = (MangaTipo) reader[8]
            };
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

        private static void DotEnvSetup()
        {
            var env = Environment.GetEnvironmentVariables();
            if (!env.Contains("YURIDB_HOST")) {
                try {
                    Dictionary<string, string> vars = DotEnv.LoadFile(".env");
                    DotEnv.InjectIntoEnvironment(vars);
                }
                catch (Exception) {
                    Console.WriteLine("[Error] El archivo .dotenv no existe o es inválido");
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
