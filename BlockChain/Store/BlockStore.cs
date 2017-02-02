using Consensus;
using System.Linq;
using Store;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;

namespace BlockChain.Store
{
	public enum LocationEnum
	{
		Genesis = 1,
		Main = 2,
		Branch = 3,
		Orphans = 4
	}

	public class BlockStore : ConsensusTypeStore<Types.BlockHeader>
	{
		private const string BLOCK_HEADERS = "bk-headers";
		private const string CHILDREN = "bk-children";
		private const string LOCATIONS = "bk-locations";
		private const string TOTAL_WORK = "bk-totalwork";
		private const string BLOCK_TRANSACTIONS = "bk-transactions";
		private const string TRANSACTIONS = "transactions";
		public Store<Types.Transaction> TxStore { get; private set; }

		public BlockStore() : base(BLOCK_HEADERS)
		{
			TxStore = new ConsensusTypeStore<Types.Transaction>(TRANSACTIONS);
		}

		public void Put(TransactionContext transactionContext, Keyed<Types.Block> block, LocationEnum location, double totalWork)
		{
			base.Put(transactionContext, new Keyed<Types.BlockHeader>(block.Key, block.Value.header));

			//children
			var children = new HashSet<byte[]>();
			children.Add(block.Key);

			transactionContext.Transaction.InsertHashSet<byte[], byte[]>(
				CHILDREN,
				block.Value.header.parent,
				children,
				0,
				false
			);

			//location
			SetLocation(transactionContext, block.Key, location);

			//total work
			transactionContext.Transaction.Insert<byte[], double>(TOTAL_WORK, block.Key, totalWork);

			//transactions
			var transactionsSet = new HashSet<byte[]>();

			foreach (var tx in block.Value.transactions)
			{
				var txHash = Merkle.transactionHasher.Invoke(tx);

				transactionsSet.Add(txHash);

				if (!TxStore.ContainsKey(transactionContext, txHash))
				{
					TxStore.Put(transactionContext, new Keyed<Types.Transaction>(txHash, tx)); 
				}
			}

			transactionContext.Transaction.InsertHashSet<byte[], byte[]>(
				BLOCK_TRANSACTIONS,
				block.Key,
				transactionsSet,
				0,
				false
			);
		}

		public Keyed<Types.Block> GetBlock(TransactionContext transactionContext, byte[] key)
		{
			var header = base.Get(transactionContext, key);
			var txs = new List<Types.Transaction>();

			foreach (var tx in transactionContext.Transaction.SelectHashSet<byte[], byte[]>(BLOCK_TRANSACTIONS, key, 0))
			{
				txs.Add(TxStore.Get(transactionContext, tx).Value);
			}

			var block = new Types.Block(header.Value, ListModule.OfSeq<Types.Transaction>(txs));

			return new Keyed<Types.Block>(key, block);
		}

		public bool IsLocation(TransactionContext transactionContext, byte[] item, LocationEnum location)
		{
			return transactionContext.Transaction.Select<byte[], int>(LOCATIONS, item).Value == (int) location;
		}

		public LocationEnum GetLocation(TransactionContext transactionContext, byte[] item)
		{
			return (LocationEnum) transactionContext.Transaction.Select<byte[], int>(LOCATIONS, item).Value;
		}

		public void SetLocation(TransactionContext transactionContext, byte[] item, LocationEnum location)
		{
			transactionContext.Transaction.Insert<byte[], int>(LOCATIONS, item, (int)location);
		}

		public double TotalWork(TransactionContext transactionContext, byte[] item)
		{
			return transactionContext.Transaction.Select<byte[], double>(TOTAL_WORK, item).Value;
		}

		public IEnumerable<Keyed<Types.Block>> Children(TransactionContext transactionContext, byte[] parent, bool orphans)
		{
			foreach (var child in transactionContext.Transaction.SelectHashSet<byte[], byte[]>(CHILDREN, parent, 0))
			{
				if (orphans == IsLocation(transactionContext, child, LocationEnum.Orphans))
				{
					yield return GetBlock(transactionContext, child);
				}
			}
		}

		public IEnumerable<Keyed<Types.Transaction>> Transactions(TransactionContext transactionContext, byte[] block)
		{
			foreach (var txHash in transactionContext.Transaction.SelectHashSet<byte[], byte[]>(BLOCK_TRANSACTIONS, block, 0))
			{
				yield return TxStore.Get(transactionContext, txHash);
			}
		}

		public bool HasChildren(TransactionContext transactionContext, byte[] parent)
		{
			var hashSet = transactionContext.Transaction.SelectHashSet<byte[], byte[]>(CHILDREN, parent, 0);

			return hashSet.Count() > 0;
		}
	}
}