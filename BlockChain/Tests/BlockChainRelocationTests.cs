using Consensus;
using BlockChain.Store;
using Store;
using NUnit.Framework;
using BlockChain.Data;
using Infrastructure.Testing;

namespace BlockChain.Tests
{
	//public static class TagHelper
	//{
	//	public static HashDictionary<string> values = new HashDictionary<string>();

	//	public static string GetValue(byte[] key)
	//	{
	//		return values[key];
	//	}
	//}

	[TestFixture()]
	public class BlockChainRelocationTests : BlockChainTestsBase
	{
		private Types.Block block1;
		private Types.Block block2;
		private Types.Block block3;

		[OneTimeSetUp]
		public new void OneTimeSetUp()
		{
			base.OneTimeSetUp();
			block1 = _GenesisBlock.Value.Child();
			block2 = _GenesisBlock.Value.Child();
			block3 = block2.Child();

			//_GenesisBlock.SetTag("g");
			//block1.SetTag("1");
			//block2.SetTag("2");
			//block3.SetTag("3");

			//TagHelper.values[_GenesisBlock.Key] = "g";
			//TagHelper.values[block1.Key] = "1";
			//TagHelper.values[block2.Key] = "2";
			//TagHelper.values[block3.Key] = "3";

			//System.Console.WriteLine("g: " + (int)_GenesisBlock.Key[0]);
			//System.Console.WriteLine("1: " + (int)block1.Key[0]);
			//System.Console.WriteLine("2: " + (int)block2.Key[0]);
			//System.Console.WriteLine("3: " + (int)block3.Key[0]);
		}

		[Test, Order(1)]
		public void ShouldAddBlocks()
		{
			Assert.That(_BlockChain.HandleNewBlock(_GenesisBlock.Value), Is.EqualTo(AddBk.Result.Added));
			Assert.That(Location(_GenesisBlock.Value), Is.EqualTo(LocationEnum.Main));
			Assert.That(_BlockChain.Tip.Key, Is.EqualTo(_GenesisBlock.Key));

			Assert.That(_BlockChain.HandleNewBlock(block1), Is.EqualTo(AddBk.Result.Added));
			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Main));
			Assert.That(_BlockChain.Tip.Value.Equals(block1.header), Is.True);

			Assert.That(_BlockChain.HandleNewBlock(block2), Is.EqualTo(AddBk.Result.Added));
			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Branch));
			Assert.That(_BlockChain.Tip.Value.Equals(block1.header), Is.True);
		}

		[Test, Order(2)]
		public void ShouldReorganize()
		{
			Assert.That(_BlockChain.HandleNewBlock(block3), Is.EqualTo(AddBk.Result.Added));

			Assert.That(Location(block1), Is.EqualTo(LocationEnum.Branch));
			Assert.That(Location(block2), Is.EqualTo(LocationEnum.Main));
			Assert.That(Location(block3), Is.EqualTo(LocationEnum.Main));
			Assert.That(_BlockChain.Tip.Value.Equals(block3.header), Is.True);
		}
	}
}