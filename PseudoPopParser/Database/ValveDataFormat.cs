using System.Collections.Generic;

namespace PseudoPopParser.Database {

	enum VDFValueType {
		STRING,
		BLOCK
	}

	class VDFTree {
		public VDFNode Head, Current;

		public VDFTree() { }

		public void AddKeyValue(string key, string value) {
			VDFNode newNode = new VDFNode(this.Current, key, new VDFString(value));
			if (this.Head == null) {
				this.Head = newNode;
				this.Current = this.Head;
			}
			else {
				((List<VDFNode>)this.Current.Value).Add(newNode);
			}
		}

		public void StepBlock(string blockName) {
			VDFNode newNode = new VDFNode(this.Current, blockName, new VDFBlock());
			if (this.Head == null) {
				this.Head = newNode;
			}
			else {
				((List<VDFNode>)this.Current.Value).Add(newNode);
			}
			this.Current = newNode; // Step into block
		}

		public void StepOut() {
			this.Current = this.Current.Parent; // Step out of block
		}
	}

	class VDFNode {
		public override string ToString() => this.Key + " : " + this.Data.ToString();
		public VDFNode Parent { get; set; } = null;
		public string Key { get; set; }
		protected VDFData Data;

		public VDFNode(VDFNode parent, string key, VDFData value) {
			this.Parent = parent;
			this.Key = key;
			this.Data = value;
		}

		public dynamic Value {
			get {
				if (this.Data is VDFString) {
					return ((VDFString)this.Data).String;
				}
				else if (this.Data is VDFBlock) {
					return ((VDFBlock)this.Data).List;
				}
				throw new System.Exception("Case not found");
			}
			set {
				if (this.Data is VDFString) {
					((VDFString)this.Data).String = value;
				}
				throw new System.Exception("Case not found");
			}
		}

		public int SearchKey(string key) {
			if (!(this.Data is VDFBlock)) return -1;

			string upperKey = key.ToUpper();
			VDFBlock BlockData = (VDFBlock)this.Data;
			for (int i = 0; i < BlockData.List.Count; i++) {
				if (BlockData.List[i].Key.ToUpper() == upperKey) {
					return i;
				}
			}
			return -1;
		}

		public string[] GetBlockKeys() {
			List<VDFNode> blockNodes = ((VDFBlock)this.Data).List;
			List<string> keys = new List<string>();
			foreach (VDFNode node in blockNodes) {
				keys.Add(node.Key);
			}
			return keys.ToArray();
		}

		public bool KeyExists(string key) {
			int keyIndex = SearchKey(key);
			return keyIndex > -1;
		}

		public VDFNode GetNode(string key) {
			int keyIndex = SearchKey(key);
			if (keyIndex == -1) throw new KeyNotFoundException();
			VDFBlock BlockData = (VDFBlock)this.Data;
			return BlockData.List[keyIndex];
		}

		public dynamic GetValueFromKey(string key) {
			VDFNode Node = GetNode(key);
			return Node.Value;
		}
	}

	abstract class VDFData {
		public virtual VDFValueType Type { get; protected set; }
	}

	class VDFString : VDFData {
		public override VDFValueType Type { get => VDFValueType.STRING; }
		public override string ToString() => this.String;
		public string String { get; set; }
		public VDFString(string value) => this.String = value;
	}

	class VDFBlock : VDFData {
		public override VDFValueType Type { get => VDFValueType.BLOCK; }
		public override string ToString() => "Block[" + this.List.Count + ']';
		public List<VDFNode> List { get; private set; }
		public VDFBlock() => this.List = new List<VDFNode>();
		public void AddNode(VDFNode node) => this.List.Add(node);
	}
}