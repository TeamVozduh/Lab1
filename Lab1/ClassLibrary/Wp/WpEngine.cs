namespace ClassLibrary.Wp;

/// <summary>Contains the calculated goal, weakest precondition, derivation, and Hoare triple.</summary>
/// <param name="Post">Normalized symbolic postcondition.</param>
/// <param name="PostHumanReadable">Postcondition explained in Russian.</param>
/// <param name="WeakestPrecondition">Normalized symbolic precondition.</param>
/// <param name="WeakestPreconditionHumanReadable">Precondition explained in Russian.</param>
/// <param name="Trace">Numbered backward-calculation steps.</param>
/// <param name="Triad">Formatted Hoare triple and its plain-language meaning.</param>
public sealed record WpResult(
    string Post,
    string PostHumanReadable,
    string WeakestPrecondition,
    string WeakestPreconditionHumanReadable,
    string Trace,
    string Triad);

/// <summary>Calculates symbolic weakest preconditions for loop-free fragments.</summary>
public static class WpEngine
{
    /// <summary>Parses both inputs and moves the Post backward through every statement.</summary>
    /// <param name="fragment">Assignments and complete if/else statements to analyze.</param>
    /// <param name="postcondition">Condition required after the fragment finishes.</param>
    /// <returns>The wp, its derivation, and text ready for the UI.</returns>
    /// <exception cref="WpParseException">The program or Post has invalid syntax.</exception>
    public static WpResult Calculate(string fragment, string postcondition)
    {
        ArgumentNullException.ThrowIfNull(fragment);
        ArgumentNullException.ThrowIfNull(postcondition);

        var program = new WpParser(fragment).ParseProgram();
        var parsedPost = new WpParser(postcondition).ParsePostcondition();
        var post = WpLogic.Simplify(parsedPost);
        var trace = new TraceWriter();
        trace.Add("Цель (Post): " + WpLogic.Format(post));

        // A partial expression in Post must be defined in the final state too.
        var postDefined = WpLogic.Defined(parsedPost);
        var target = WpLogic.And(postDefined, post);
        if (postDefined is not BooleanConstant { Value: true })
            trace.Add("Определённость Post: " + WpLogic.Format(postDefined));

        var pre = PullBack(program, target, trace, string.Empty);
        var preText = WpLogic.Format(pre);
        var postText = WpLogic.Format(post);
        var preExplanation = WpLogic.Explain(pre);
        var postExplanation = WpLogic.Explain(post);
        trace.Add("Итоговое wp: " + preText);

        return new WpResult(
            postText,
            "После выполнения операции каршеринга:" + Environment.NewLine + postExplanation,
            preText,
            "До выполнения операции каршеринга требуется:" + Environment.NewLine + preExplanation,
            trace.ToString(),
            "{ " + preText + " } " + fragment.Trim() + " { " + postText + " }" +
            Environment.NewLine + "Смысл для каршеринга: если перед выполнением верно:" +
            Environment.NewLine + preExplanation + Environment.NewLine +
            "то после выполнения будет верно:" + Environment.NewLine + postExplanation);
    }

    /// <summary>Explains a valid Post while the user edits it, without analyzing a program.</summary>
    public static string DescribePostcondition(string postcondition)
    {
        ArgumentNullException.ThrowIfNull(postcondition);
        var parsed = new WpParser(postcondition).ParsePostcondition();
        return "После выполнения операции каршеринга:" + Environment.NewLine +
               WpLogic.Explain(WpLogic.Simplify(parsed));
    }

    /// <summary>Applies assignment and branch wp rules in reverse execution order.</summary>
    private static BooleanExpression PullBack(IReadOnlyList<Statement> statements,
                                              BooleanExpression target, TraceWriter trace,
                                              string indentation)
    {
        var current = target;
        for (var i = statements.Count - 1; i >= 0; i--)
        {
            switch (statements[i])
            {
                case AssignmentStatement assignment:
                {
                    // An assignment must be defined; replace its target variable
                    // with the assigned expression in the later condition.
                    var defined = WpLogic.Defined(assignment.Value);
                    var substituted = WpLogic.Substitute(current, assignment.Variable, assignment.Value);
                    current = WpLogic.And(defined, substituted);
                    trace.Add(indentation + "Через " + assignment.Variable + " := " +
                              WpLogic.Format(assignment.Value) + " → " + WpLogic.Format(current));
                    if (defined is not BooleanConstant { Value: true })
                        trace.Add(indentation + "  Условие определённости: " + WpLogic.Format(defined));
                    break;
                }

                case IfStatement branch:
                {
                    // Compute both branches against the same target, then select
                    // each result under its guard and require a valid guard.
                    var guard = WpLogic.Simplify(branch.Condition);
                    trace.Add(indentation + "Через if (" + WpLogic.Format(guard) +
                              "): считаем обе ветки от цели " + WpLogic.Format(current));
                    trace.Add(indentation + "  Ветка if:");
                    var thenPre = PullBack(branch.ThenBranch, current, trace, indentation + "    ");
                    if (branch.ThenBranch.Count == 0)
                        trace.Add(indentation + "    Пустая ветка → " + WpLogic.Format(thenPre));
                    trace.Add(indentation + "  Ветка else:");
                    var elsePre = PullBack(branch.ElseBranch, current, trace, indentation + "    ");
                    if (branch.ElseBranch.Count == 0)
                        trace.Add(indentation + "    Пустая ветка → " + WpLogic.Format(elsePre));

                    var guardedBranches = WpLogic.Or(
                        WpLogic.And(guard, thenPre), WpLogic.And(WpLogic.Not(guard), elsePre));
                    var guardDefined = WpLogic.Defined(branch.Condition);
                    current = WpLogic.And(guardDefined, guardedBranches);
                    trace.Add(indentation + "После if: " + WpLogic.Format(current));
                    if (guardDefined is not BooleanConstant { Value: true })
                        trace.Add(indentation + "  Условие определённости проверки: " +
                                  WpLogic.Format(guardDefined));
                    break;
                }

                default:
                    throw new InvalidOperationException("Неизвестный оператор программы");
            }
        }
        return current;
    }

    /// <summary>Numbers derivation steps in the order generated by backward traversal.</summary>
    private sealed class TraceWriter
    {
        private readonly List<string> steps = new();

        /// <summary>Appends a numbered step to the derivation.</summary>
        public void Add(string text) => steps.Add($"{steps.Count + 1}. {text}");

        /// <summary>Joins steps into the multiline text shown by the UI.</summary>
        public override string ToString() => string.Join(Environment.NewLine, steps);
    }
}
