using Linq2dbPrimaryKeyDateTimeOffset.Test.Lib;
using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Linq2dbPrimaryKeyDateTimeOffset.Test;

[TestClass]
public class DateTimePrimaryKeyTest {
	private static IHost? _host;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context) {

		_host = Host
			.CreateDefaultBuilder()
			.ConfigureServices((services) => {
				services.AddLinqToDbContext<MyDB>((provider, options) => {
					options
					.UseSQLiteOfficial($"Data Source ={Path.Combine(context.TestDir, "DateTimePrimaryKeyTest.db")}; Version = 3; Pooling = true; Max Pool Size = 5; ")
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
			db.CreateTable<DefaultTimedTable>();

			db.CommitTransaction();
		} catch {
			db.RollbackTransaction();
			throw;
		}
	}

	[TestMethod]
	public async Task WithMyConverterAsync() {
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
		MyTimedTable? returned = await db.MyTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.Timestamp == theTime);
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);

		////UPDATE
		var u = new MyTimedTable() { Key = theKey, Timestamp = theTime, Name = "updated" };

		await db.InsertOrReplaceAsync(u);

	}

	[TestMethod]
	public async Task DefaultConverterAsync() {
		//Arrange
		_ = _host ?? throw new Exception("ClassInitialize not ok");
		using IServiceScope s = _host.Services.CreateScope();
		using MyDB db = s.ServiceProvider.GetRequiredService<MyDB>();

		string theKey = "MyKey";
		DateTimeOffset theTime = DateTimeOffset.UtcNow;

		//CREATE
		var t = new DefaultTimedTable() { Key = theKey, Timestamp = theTime, Name = "First Test" };
		await db.InsertAsync(t);

		//READ
		DefaultTimedTable? returned = await db.DefaultTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.Timestamp == theTime);
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);

		////UPDATE
		var u = new DefaultTimedTable() { Key = theKey, Timestamp = theTime, Name = "updated" };

		await db.InsertOrReplaceAsync(u);
	}

	public class MyDB : DataConnection {
		public MyDB(LinqToDbConnectionOptions<MyDB> config) : base(config) { }

		public ITable<MyTimedTable> MyTimedTable => GetTable<MyTimedTable>();
		public ITable<DefaultTimedTable> DefaultTimedTable => GetTable<DefaultTimedTable>();
	}

	[Table(Name = "MyTimedTable")]
	public class MyTimedTable {
		public MyTimedTable() {

		}

		[Column("Key"), PrimaryKey, NotNull] public string Key { get; set; }

		[Column("Timestamp"), PrimaryKey, NotNull][DateTimeOffsetConverter] public DateTimeOffset Timestamp { get; set; }

		[Column("Name")] public string? Name { get; set; }
	}

	[Table(Name = "DefaultTimedTable")]
	public class DefaultTimedTable {
		public DefaultTimedTable() {

		}

		[Column("Key"), PrimaryKey, NotNull] public string Key { get; set; }

		[Column("Timestamp"), PrimaryKey, NotNull] public DateTimeOffset Timestamp { get; set; }

		[Column("Name")] public string? Name { get; set; }
	}

}