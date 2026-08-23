# Replace Nested Conditional with Guard Clauses

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Conditional Expressions
Replace Nested Conditional withGuard Clauses
Problem
You have a group of nested conditionals and it is hard to determine the normal flow of code
execution.
Solution
Isolate all special checks and edge cases into separate clauses and place them before the main
checks. Ideally, you should have a “flat” list of conditionals, one after the other.
public double getPayAmount() {
  double result;
  if (isDead){
    result = deadAmount();
  }
  else {
    if (isSeparated){
      result = separatedAmount();
    }
    else {
      if (isRetired){
        result = retiredAmount();
      }
      else{
        result = normalPayAmount();
      }
    }
  }
Before Java C# PHP Python TypeScript

 
23.08.2026, 11:11 Replace Nested Conditional with Guard Clauses
https://sourcemaking.com/refactoring/replace-nested-conditional-with-guard-clauses 1/3

Why Refactor
Spotting the “conditional from hell” is fairly easy. The indentations of each level of nestedness form
an arrow, pointing to the right in the direction of pain and woe:
if () {
    if () {
        do {
            if () {
                if () {
                    if () {
                        ...
                    }
                }
                ...
            }
            ...
        }
        while ();
        ...
    }
    else {
        ...
  return result;
}
public double getPayAmount() {
  if (isDead){
    return deadAmount();
  }
  if (isSeparated){
    return separatedAmount();
  }
  if (isRetired){
    return retiredAmount();
  }
  return normalPayAmount();
}
After
23.08.2026, 11:11 Replace Nested Conditional with Guard Clauses
https://sourcemaking.com/refactoring/replace-nested-conditional-with-guard-clauses 2/3

    }
}
It is difficult to figure out what each conditional does and how, since the “normal” flow of code
execution is not immediately obvious. These conditionals indicate helter-skelter evolution, with
each condition added as a stopgap measure without any thought paid to optimizing the overall
structure.
To simplify the situation, isolate the special cases into separate conditions that immediately end
execution and return a null value if the guard clauses are true. In effect, your mission here is to
make the structure flat.
How to Refactor
Try to rid the code of side effects – Separate Query from Modifier may be helpful for the purpose.
This solution will be necessary for the reshuffling described below.
1. Isolate all guard clauses that lead to calling an exception or immediate return of a value from
the method. Place these conditions at the beginning of the method.
2. After rearrangement is complete and all tests are successfully completed, see whether you can
use Consolidate Conditional Expression for guard clauses that lead to the same exceptions or
returned values.
RETURN READ NEXT
Replace Conditional with Polymorphism    Remove Control Flag
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
23.08.2026, 11:11 Replace Nested Conditional with Guard Clauses
https://sourcemaking.com/refactoring/replace-nested-conditional-with-guard-clauses 3/3
