using System.Collections.Generic;

namespace PseudoPopParser {

	public class TreeNode<T> {

		private T data = default(T);
		private TreeNode<T> parent = default(TreeNode<T>);
		private List<TreeNode<T>> children = new List<TreeNode<T>>();

		public TreeNode() { }

		public TreeNode(T data) {
			this.data = data;
		}

		public T Value {
			get {
				return data;
			}
			set {
				data = value;
			}
		}

		public TreeNode<T> Parent {
			get {
				return parent;
			}
			set {
				parent = value;
			}
		}

		public void Add(TreeNode<T> node) {
			node.Parent = this;
			children.Add(node);
		}

		public List<TreeNode<T>> Children {
			get {
				return children;
			}
		}
	}
}
