# Replace Error Code with Exception

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Simplifying Method Calls
Replace Error Code withException
Problem
A method returns a special value that indicates an error?
Solution
Throw an exception instead.
int withdraw(int amount) {
  if (amount > _balance) {
    return -1;
  }
  else {
    balance -= amount;
    return 0;
  }
}
void withdraw(int amount) throws BalanceException {
  if (amount > _balance) {
    throw new BalanceException();
  }
  balance -= amount;
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:12 Replace Error Code with Exception
https://sourcemaking.com/refactoring/replace-error-code-with-exception 1/3

Why Refactor
Returning error codes is an obsolete holdover from procedural programming. In modern
programming, error handling is performed by special classes, which are named exceptions. If a
problem occurs, you “throw” an error, which is then “caught” by one of the exception handlers.
Special error-handling code, which is ignored in normal conditions, is activated to respond.
Benefits
Frees code from a large number of conditionals for checking various error codes. Exception
handlers are a much more succinct way to differentiate normal execution paths from abnormal
ones.
Exception classes can implement their own methods, thus containing part of the error handling
functionality (such as for sending error messages).
Unlike exceptions, error codes cannot be used in a constructor, since a constructor must return
only a new object.
Drawbacks
An exception handler can turn into a goto-like crutch. Avoid this! Do not use exceptions to manage
code execution. Exceptions should be thrown only to inform of an error or critical situation.
How to Refactor
Try to perform these refactoring steps for only one error code at a time. This will make it easier to
keep all the important information in your head and avoid errors.
1. Find all calls to a method that returns error codes and, instead of checking for an error code,
wrap it in try/catch blocks.
2. Inside the method, instead of returning an error code, throw an exception.
3. Change the method signature so that it contains information about the exception being thrown
(@throws section).
23.08.2026, 11:12 Replace Error Code with Exception
https://sourcemaking.com/refactoring/replace-error-code-with-exception 2/3

RETURN READ NEXT
Replace Exception with Test    Replace Constructor with Factory
Method
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
23.08.2026, 11:12 Replace Error Code with Exception
https://sourcemaking.com/refactoring/replace-error-code-with-exception 3/3
