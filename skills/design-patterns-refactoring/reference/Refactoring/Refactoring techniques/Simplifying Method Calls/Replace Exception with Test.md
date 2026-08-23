# Replace Exception with Test

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Replace Exception with Test
Problem
You throw an exception in a place where a simple test would do the job?
Solution
Replace the exception with a condition test.
Why Refactor
double getValueForPeriod(int periodNumber) {
  try {
    return values[periodNumber];
  } catch (ArrayIndexOutOfBoundsException e) {
    return 0;
  }
}
double getValueForPeriod(int periodNumber) {
  if (periodNumber >= values.length) {
    return 0;
  }
  return values[periodNumber];
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:12 Replace Exception with Test
https://sourcemaking.com/refactoring/replace-exception-with-test 1/2

Exceptions should be used to handle irregular behavior related to an unexpected error. They should
not serve as a replacement for testing. If an exception can be avoided by simply verifying a
condition before running, then do so. Exceptions should be reserved for real errors.
For instance, you entered a minefield and triggered a mine there, resulting in an exception; the
exception was successfully handled and you were lifted through the air to safety beyond the mine
field. But you could have avoided this all by simply reading the warning sign in front of the
minefield to begin with.
Benefits
A simple conditional can sometimes be more obvious than exception handling code.
How to Refactor
1. Create a conditional for an edge case and move it before the try/catch block.
2. Move code from the catch section inside this conditional.
3. In the catch section, place the code for throwing a usual unnamed exception and run all the
tests.
4. If no exceptions were thrown during the tests, get rid of the try/catch operator.
RETURN READ NEXT
Dealing with Generalisation    Replace Error Code with Exception
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
23.08.2026, 11:12 Replace Exception with Test
https://sourcemaking.com/refactoring/replace-exception-with-test 2/2
