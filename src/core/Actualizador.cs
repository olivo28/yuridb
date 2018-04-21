using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using YuriDatabase = YuriDb.Core.YuriDb;
using MySql.Data.MySqlClient;

namespace YuriDb.Core 
{
	public class Actualizador 
	{
		private static Actualizador _instancia;
		private static object _lock = new object();
		private static TmoClient _cliente;
		private static YuriDatabase _db;


		private Actualizador()
		{
			_cliente = new TmoClient();
			_db = YuriDatabase.Instancia;
		}

		public delegate Task AddMangaHandler(MangaYuri manga, uint index, uint remaining);

		private AddMangaHandler _onAdd;
		public event AddMangaHandler OnAdd
		{
			add
			{
				lock(_lock)
				{
					_onAdd += value;
				}
			}
			remove
			{
				lock(_lock)
				{
					_onAdd -= value;
				}
			}
		}

		private volatile bool _isUpdating;
		private volatile uint _updated;
		private volatile uint _remaining;
		private volatile bool _isRateLimitExceeded;

		public bool IsUpdating {
			get 
			{
				return _isUpdating;
			}
		}

		public bool IsRateLimitExceeded
		{
			get
			{
				return _isRateLimitExceeded;
			}
		}
		
		private uint _run()
		{
			_isUpdating = true;
			_updated = 0;
			_remaining = 0;
			var page = _cliente.GetPagina(TmoClient.MangasYuri, 1, 1);
			uint countBd = (uint) _db.GetCantidadMangas();
			
			_remaining = page.Count() - countBd;
			
			if (_remaining == 0) {
				_stop();
				return 0;
			}

			_waitFor(page);
			page = _cliente.GetPagina(TmoClient.MangasYuri, countBd / 60 + 1, 60);
			_agregar(page, countBd % 60);
			while (page.HasNext() && _isUpdating) {
				_waitFor(page);
				page = _cliente.GetPagina(TmoClient.MangasYuri, page.NextPage(), 60);
				_agregar(page);
			}
			_stop();
			return _updated;
		}

		private void _waitFor(TmoPage page) 
		{
			if (page.Remaining != 0) {
				return;
			} 
			_isRateLimitExceeded = true;
			TimeSpan waitTime = page.WaitTime();
			while (waitTime.Ticks > 0 && _isUpdating) {
				waitTime -= TimeSpan.FromMilliseconds(17);
				Thread.Sleep(17);
			}
			if (_isUpdating) {
				page.Wait();
			}
			_isRateLimitExceeded = false;
		}

		private void _agregar(TmoPage page, uint index = 0)
		{
            JArray data = (JArray) page.Data["data"];
            for (int i = (int) index; i < data.Count; i++) {
            	bool duplicated = false;
            	_waitFor(page);
            	if(!_isUpdating) {
            		return;
            	}
                JObject obj = (JObject) data[i];
                JObject info = (JObject) obj["info"];
                MangaYuri manga = new MangaYuri(
                	(UInt32) obj["id"],
                    DateTime.Parse((string) info["fechaCreacion"])
                ) {
                    Nombre = (string) obj["nombre"],
                    Descripcion = (string) info["sinopsis"],
                    Imagen = new Uri("https://img1.tumangaonline.com/" + (string) obj["imageUrl"]),
                	Tipo = (MangaTipo) Enum.Parse(typeof(MangaTipo), (string) obj["tipo"], true),
                	Estado = (MangaEstado) Enum.Parse(typeof(MangaEstado), (string) obj["estado"], true)
                };
                TmoPage capi = _cliente.GetPagina(TmoClient.ToUriManga(manga.TmoId.Value), 1, 1, page);
                manga.Capitulos = (UInt32) capi.Data["total"];
                try {
                	_db.AgregarManga(manga);
                } catch(MySqlException e) {
					if (e.Number != (int) MySqlErrorCode.DuplicateKeyEntry) {
						throw e;
					}
					duplicated = true;
                }
                _updated++;
                _remaining--;
                
                lock (_lock) {
                	if (_onAdd != null && !duplicated) {
                		_onAdd(manga, _updated - 1, _remaining);
                	}
                }
            }
		}

		private void _stop()
		{
			_isUpdating = false;
		}

		public Task<uint> RunAsync()
		{
			return Task.Run<uint>((Func<uint>) _run);
		}

		public Task StopAsync()
		{
			return Task.Run((Action) _stop);
		}

		public static Actualizador Instancia
		{
			get 
			{
				lock(_lock) {
					if (_instancia == null) {
						_instancia = new Actualizador();
					}
					return _instancia;
				}
			}
		}
	}
}