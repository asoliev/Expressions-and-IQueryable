using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

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

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);
                return node;
            }
            else if (node.Method.DeclaringType == typeof(string))
            {
                Visit(node.Object);

                switch (node.Method.Name)
                {
                    case nameof(string.Equals):
                        _resultStringBuilder.Append("(");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append(")");
                        break;

                    case nameof(string.StartsWith):
                        _resultStringBuilder.Append("(");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append("*)");
                        break;

                    case nameof(string.EndsWith):
                        _resultStringBuilder.Append("(*");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append(")");
                        break;

                    case nameof(string.Contains):
                        _resultStringBuilder.Append("(*");
                        Visit(node.Arguments[0]);
                        _resultStringBuilder.Append("*)");
                        break;

                    default:
                        throw new NotSupportedException($"Method '{node.Method.Name}' is not supported");
                }

                return node;
            }
            else
            {
                return base.VisitMethodCall(node);
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    Expression left = node.Left;
                    Expression right = node.Right;

                    if (left.NodeType == ExpressionType.Constant && right.NodeType == ExpressionType.MemberAccess)
                    {
                        left = node.Right;
                        right = node.Left;
                    }

                    if (left.NodeType != ExpressionType.MemberAccess)
                        throw new NotSupportedException($"Left operand should be property or field: {node.NodeType}");

                    if (right.NodeType != ExpressionType.Constant)
                        throw new NotSupportedException($"Right operand should be constant: {node.NodeType}");

                    Visit(left);
                    _resultStringBuilder.Append("(");
                    Visit(right);
                    _resultStringBuilder.Append(")");
                    break;

                case ExpressionType.AndAlso:
                    _resultStringBuilder.Append("(");
                    Visit(node.Left);
                    _resultStringBuilder.Append(" AND ");
                    Visit(node.Right);
                    _resultStringBuilder.Append(")");
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

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