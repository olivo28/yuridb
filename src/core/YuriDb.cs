using MySql.Data.MySqlClient;
using System;

namespace YuriDb
{
    public enum MangaEstado : byte
    {
        EnEmision,
        Finalizado
    }

    public enum MangaTipo : byte
    {
        Manga,
        Manhua,
        Manhwa,
        Novela,
        Propio,
        Otro
    }

    public class MangaYuri
    {
        public uint Id { get; private set; }
        public uint TmoId { get; private set; }
        public DateTime TmoCreacion { get; private set; } = DateTime.Now;
        public uint Capitulos { get; set; }
        public Uri Imagen { get; set; } 
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public MangaEstado Estado { get; set; }
        public MangaTipo Tipo { get; set; }

        public MangaYuri(uint id, uint tmoId, DateTime tmoCreacion)
        {
            Id = id;
            TmoId = tmoId;
            TmoCreacion = tmoCreacion;
            Capitulos = 0;
        }

        public MangaYuri(uint tmoId, DateTime tmoCreacion) : 
            this(0, tmoId, tmoCreacion)
        {
        }

    }

    public class YuriDb
    {

        public bool Connected { get; private set; } 
        public string Password { get; set; }
        public string Host { get; set; }
        public string User { get; set; }
        public string Db   { get; set; }
        public int Port    { get; set; }
        public MySqlConnection Connection { get; private set; }

        public void CreateConnection()
        {
            if (Connection == null) {
                Connection = new MySqlConnection($"SERVER={Host};PORT={Port};UID={User};PASSWORD={Password}");
            }
        }

        public void OpenConnection()
        {
            if (!Connected) {
                Connection.Open();
                Connected = true;
            }
        }

        public void Close()
        {
            if (Connection == null)
                return;
            if (Connected) 
                Connection.Close();
        }

        public void Dispose()
        {
            if (Connection == null)
                return;
            Connection.Dispose();
            Connection = null;
        }

        public void CrearBaseDeDatos()
        {
            MySqlCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE IF NOT EXISTS `{Db}` CHARACTER SET = 'utf8'";
            cmd.ExecuteNonQuery();
        }

        public void CrearTablas()
        {
            MySqlCommand cmd = Connection.CreateCommand();
            cmd.CommandText = 
            string.Format(
@"CREATE TABLE IF NOT EXISTS `{0}`.`mangas` (
id              INT UNSIGNED        AUTO_INCREMENT PRIMARY KEY,
tmoId           INT UNSIGNED        NOT NULL UNIQUE KEY,
tmoCreación     DATE                NOT NULL COMMENT 'Se usa internamente para búsqueda binaria',
nombre          VARCHAR(512)        NOT NULL,
imagen          VARCHAR(256)        NULL,
descripción     VARCHAR(1024)       NULL,
capítulos       INT UNSIGNED        NOT NULL DEFAULT 0,
estado          TINYINT UNSIGNED    NOT NULL DEFAULT 0,
tipo            TINYINT UNSIGNED    NOT NULL DEFAULT 0,
FULLTEXT INDEX (nombre)
) ENGINE = MyIsam, COMMENT = 'Guarda referencias y la información de los mangas extraídos de TMO'",
            Db);
            cmd.ExecuteNonQuery();
        }

        public void AgregarManga(MangaYuri manga)
        {
            if (manga == null) {
                throw new ArgumentNullException("manga es nulo");
            }

            if (manga.TmoId < 0) {
                throw new ArgumentException("tmoId no puede ser negativo");
            }

            if (manga.Nombre == null || manga.Nombre.Length == 0) {
                throw new ArgumentException("el nombre es nulo o vacío");
            }


            MySqlCommand cmd = Connection.CreateCommand();
            cmd.CommandText = 
            string.Format(
 @"INSERT INTO `{0}`.`mangas` (
    tmoId, tmoCreación, nombre, descripción, capítulos, imagen, tipo, estado
) values (
    @tmoId, @tmoCreación, @nombre, @descripción, @capítulos, @imagen, @tipo, @estado
)", 
            Db);
            cmd.Parameters.AddWithValue("@tmoId", manga.TmoId);
            cmd.Parameters.AddWithValue("@tmoCreación", manga.TmoCreacion.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@nombre", manga.Nombre);
            cmd.Parameters.AddWithValue("@descripción", manga.Descripcion ?? string.Empty);
            cmd.Parameters.AddWithValue("@capítulos", manga.Capitulos);
            cmd.Parameters.AddWithValue("@imagen", manga.Imagen == null ? "NULL" : manga.Imagen.ToString());
            cmd.Parameters.AddWithValue("@tipo", manga.Tipo);
            cmd.Parameters.AddWithValue("@estado", manga.Estado);
            cmd.Prepare();
            cmd.ExecuteNonQuery();
        }

        public long GetCantidadMangas()
        {
            MySqlCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM `{Db}`.`mangas`";
            return (Int64) cmd.ExecuteScalar();
        }

        public MangaYuri GetUltimaAgregacion()
        {
            MySqlCommand cmd = Connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM `{Db}`.`mangas` ORDER BY `tmoCreación` DESC LIMIT 1";
            MySqlDataReader reader = cmd.ExecuteReader();
            reader.Read();
            MangaYuri manga = new MangaYuri(
                (UInt32) reader[0],
                (UInt32) reader[1],
                (DateTime) reader[2]
            ) {
                Nombre = (string) reader[3],
                Imagen = new Uri((string) reader[4]),
                Descripcion = (string) reader[5],
                Capitulos = (UInt32) reader[6],
                Estado = (MangaEstado) reader[7],
                Tipo = (MangaTipo) reader[8]
            };
            reader.Close();
            return manga;
        }

    }
}
