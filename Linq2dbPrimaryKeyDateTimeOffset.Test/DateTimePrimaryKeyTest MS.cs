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
public class DateTimePrimaryKeyTestMS {
	private static IHost? _host;

	[ClassInitialize]
	public static void ClassInitialize(TestContext context) {

		_host = Host
			.CreateDefaultBuilder()
			.ConfigureServices((services) => {
				services.AddLinqToDbContext<MyDB>((provider, options) => {
					options
						.UseSQLiteMicrosoft($"Data Source ={Path.Combine(context.TestDir, "DateTimePrimaryKeyTestMS.db")}; Pooling = true;  ")
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
		var t = new MyTimedTable(theKey, theTime) { Name = "First Test" };
		await db.InsertAsync(t);

		//READ
		MyTimedTable? returned = await db.MyTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.Timestamp == theTime);
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);

		////UPDATE
		var u = new MyTimedTable(theKey, theTime) { Name = "updated" };

		await db.InsertOrReplaceAsync(u);

		//READAGAIN
		returned = await db.MyTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.Timestamp == theTime);
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);
		Assert.AreEqual(u.Name, returned.Name);

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
		var t = new DefaultTimedTable(theKey, theTime) { Name = "First Test" };
		await db.InsertAsync(t);

		//READ
		DefaultTimedTable? returned = await db.DefaultTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.Timestamp == theTime);
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);

		////UPDATE
		var u = new DefaultTimedTable(theKey, theTime) { Name = "updated" };

		await db.InsertOrReplaceAsync(u);

		//READAGAIN
		returned = await db.DefaultTimedTable.FirstOrDefaultAsync(x => x.Key == theKey && x.Timestamp == theTime);
		Assert.IsNotNull(returned);
		Assert.AreEqual(theKey, returned.Key);
		Assert.AreEqual(theTime, returned.Timestamp);
		Assert.AreEqual(u.Name, returned.Name);
	}

	public class MyDB : DataConnection {
		public MyDB(LinqToDbConnectionOptions<MyDB> config) : base(config) { }

		public ITable<MyTimedTable> MyTimedTable => GetTable<MyTimedTable>();
		public ITable<DefaultTimedTable> DefaultTimedTable => GetTable<DefaultTimedTable>();
	}

	public class MyTimedTable {
		public MyTimedTable(string key, DateTimeOffset timestamp, string? name = null) {
			Key = key;
			Timestamp = timestamp;
			Name = name;
		}

		[Column("Key"), PrimaryKey, NotNull] public string Key { get; set; }

		[Column("Timestamp"), PrimaryKey, NotNull, DateTimeOffsetConverter] public DateTimeOffset Timestamp { get; set; }

		[Column("Name")] public string? Name { get; set; }
	}

	[Table]
	public class DefaultTimedTable {

		public DefaultTimedTable(string key, DateTimeOffset timestamp, string? name = null) {
			Key = key;
			Timestamp = timestamp;
			Name = name;
		}

		[Column("Key"), PrimaryKey, NotNull] public string Key { get; set; }

		[Column("Timestamp"), PrimaryKey, NotNull] public DateTimeOffset Timestamp { get; set; }

		[Column("Name")] public string? Name { get; set; }
	}
}