using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;

namespace Linq2dbPrimaryKeyDateTimeOffset.Test;

[TestClass]
public class WorkaroundTest {
	private static IHost? _host;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context) {

		_host = Host
			.CreateDefaultBuilder()
			.ConfigureServices((services) => {
				services.AddLinqToDbContext<MyDB>((provider, options) => {
					options
					.UseSQLiteOfficial($"Data Source ={Path.Combine(context.TestDir, "WorkAround.db")}; Version = 3; Pooling = true; Max Pool Size = 5; ")
					;
				});
			})
			.Build();

		InitDb();
	}

	private static void InitDb() {
		_ = _host ?? throw new Exception("ClassInitialize not ok");
		using IServiceScope s = _host.Services.CreateScope();
		using MyDB db = s.ServiceProvider.GetRequiredService<MyDB>();

		DataConnection.TurnTraceSwitchOn();
		DataConnection.WriteTraceLine = (s1, s2, s3) => {
			Console.WriteLine(s1);
			Console.WriteLine(s2);
			Console.WriteLine(s3);
		};

		db.BeginTransaction();
		try {
			db.CreateTable<MyTimedTable>();

			db.CommitTransaction();
		} catch {
			db.RollbackTransaction();
			throw;
		}
	}

	[TestMethod]
	public async Task WorkAroundAsync() {
		//Arrange
		_ = _host ?? throw new Exception("ClassInitialize not ok");
		using IServiceScope s = _host.Services.CreateScope();
		using MyDB db = s.ServiceProvider.GetRequiredService<MyDB>();

		string theKey = "MyKey";
		DateTimeOffset theTime = DateTimeOffset.UtcNow;

		//CREATE
		var t = new MyTimedTable() { Key = theKey, Timestamp = theTime, Name = "First Test" };
		await db.InsertAsync(t);

		//READ
		MyTimedTable? returned = await db.MyTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.TimestampStr == theTime.ToString("O"));
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);

		////UPDATE
		var u = new MyTimedTable() { Key = theKey, Timestamp = theTime, Name = "updated" };

		await db.InsertOrReplaceAsync(u);

	}

	public class MyDB : DataConnection {
		public MyDB(LinqToDbConnectionOptions<MyDB> config) : base(config) { }

		public ITable<MyTimedTable> MyTimedTable => GetTable<MyTimedTable>();

	}

	[Table(Name = "MyTimedTable")]
	public class MyTimedTable {
		private static readonly IFormatProvider _provider = CultureInfo.InvariantCulture.DateTimeFormat;
		public MyTimedTable() {

		}

		[Column("Key"), PrimaryKey, NotNull] public string Key { get; set; }

		[Column("Timestamp"), PrimaryKey, NotNull] public string TimestampStr { get; set; }
		public DateTimeOffset Timestamp {
			get {
				return DateTimeOffset.ParseExact(TimestampStr, "O", _provider);
			}
			set {
				TimestampStr = value.ToString("O", _provider);
			}
		}

		[Column("Name")] public string? Name { get; set; }
	}

}