# Consolidate Conditional Expression

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Consolidate ConditionalExpression
Problem
You have multiple conditionals that lead to the same result or action.
Solution
Consolidate all these conditionals in a single expression.
double disabilityAmount() {
  if (seniority < 2) {
    return 0;
  }
  if (monthsDisabled > 12) {
    return 0;
  }
  if (isPartTime) {
    return 0;
  }
  // Compute the disability amount.
  // ...
}
double disabilityAmount() {
  if (isNotEligibleForDisability()) {
    return 0;
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Consolidate Conditional Expression
https://sourcemaking.com/refactoring/consolidate-conditional-expression 1/3

Why Refactor
Your code contains many alternating operators that perform identical actions. It is not clear why
the operators are split up.
The main purpose of consolidation is to extract the conditional to a separate method for greater
clarity.
Benefits
Eliminates duplicate control flow code. Combining multiple conditionals that have the same
“destination” helps to show that you are doing only one complicated check leading to one
action.
By consolidating all operators, you can now isolate this complex expression in a new method
with a name that explains the conditional’s purpose.
How to Refactor
Before refactoring, make sure that the conditionals do not have any “side effects” or otherwise
modify something, instead of simply returning values. Side effects may be hiding in the code
executed inside the operator itself, such as when something is added to a variable based on the
results of a conditional.
Consolidate the conditionals in a single expression by using and and or. As a general rule when
consolidating:
Nested conditionals are joined using and.
Consecutive conditionals are joined with or.
Perform Extract Method on the operator conditions and give the method a name that reflects the
expression’s purpose.
  }
  // Compute the disability amount.
  // ...
}
23.08.2026, 11:11 Consolidate Conditional Expression
https://sourcemaking.com/refactoring/consolidate-conditional-expression 2/3

RETURN
READ NEXT
Consolidate Duplicate Conditional
Fragments    Decompose Conditional
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
Reading is boring
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:11 Consolidate Conditional Expression
https://sourcemaking.com/refactoring/consolidate-conditional-expression 3/3
