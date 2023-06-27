using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        private const string EQUALS_STR = nameof(string.Equals);
        private const string CONTAINS_STR = nameof(string.Contains);
        private const string STARTS_WITH_STR = nameof(string.StartsWith);
        private const string ENDS_WITH_STR = nameof(string.EndsWith);
        private const string WHERE_STR = "Where";

        private readonly StringBuilder _resultStringBuilder;
        private readonly string[] _partialCompareMethods = new string[]
        {
            EQUALS_STR,
            CONTAINS_STR,
            STARTS_WITH_STR,
            ENDS_WITH_STR
        };


        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        private void AppendStarIfNeeded(MethodCallExpression node, bool isleftFixed)
        {
            if (
                    node.Method.Name == CONTAINS_STR ||
                    (isleftFixed && node.Method.Name == ENDS_WITH_STR) ||
                    (!isleftFixed && node.Method.Name == STARTS_WITH_STR)
                )
            {
                _resultStringBuilder.Append('*');
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == WHERE_STR)
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }
            if (node.Method.DeclaringType == typeof(string) && _partialCompareMethods.Contains(node.Method.Name))
            {
                Visit(node.Object);
                _resultStringBuilder.Append('(');
                AppendStarIfNeeded(node, true);
                var predicate = node.Arguments[0];
                Visit(predicate);
                AppendStarIfNeeded(node, false);
                _resultStringBuilder.Append(')');

                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    var (left, right) = node.Right.NodeType == ExpressionType.Constant
                       ? (node.Left, node.Right)
                       : (node.Right, node.Left);

                    if (left.NodeType != ExpressionType.MemberAccess)
                        throw new NotSupportedException($"Left operand should be property or field: {node.NodeType}");

                    if (right.NodeType != ExpressionType.Constant)
                        throw new NotSupportedException($"Right operand should be constant: {node.NodeType}");

                    Visit(left);
                    _resultStringBuilder.Append('(');
                    Visit(right);
                    _resultStringBuilder.Append(')');
                    break;

                case ExpressionType.AndAlso:
                    Visit(node.Left);
                    _resultStringBuilder.Append(" AND ");
                    Visit(node.Right);
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(':');

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}