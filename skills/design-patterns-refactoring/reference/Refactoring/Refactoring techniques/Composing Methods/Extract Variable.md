# Extract Variable

Source: sourcemaking.com (downloaded copy)

---

 / Refactoring / Refactoring techniques / Composing Methods
Extract Variable
Problem
You have an expression that is hard to understand.
Solution
Place the result of the expression or its parts in separate variables that are self-explanatory.
void renderBanner() {
  if ((platform.toUpperCase().indexOf("MAC") > -1) &&
       (browser.toUpperCase().indexOf("IE") > -1) &&
        wasInitialized() && resize > 0 )
  {
    // do something
  }
}
void renderBanner() {
  final boolean isMacOs = platform.toUpperCase().indexOf("MAC") > -1;
  final boolean isIE = browser.toUpperCase().indexOf("IE") > -1;
  final boolean wasResized = resize > 0;
  if (isMacOs && isIE && wasInitialized() && wasResized) {
    // do something
  }
}
Before
After
Java C# PHP Python TypeScript

 
23.08.2026, 11:10 Extract Variable
https://sourcemaking.com/refactoring/extract-variable 1/3

Why Refactor
The main reason for extracting variables is to make a complex expression more understandable, by
dividing it into its intermediate parts.
These could be:
Condition of the if() operator or a part of the ?: operator in C-based languages
A long arithmetic expression without intermediate results
Long multipart lines
Extracting a variable may be the first step towards performing Extract Method if you see that the
extracted expression is used in other places in your code.
Benefits
More readable code! Try to give the extracted variables good names that announce the variable’s
purpose loud and clear. More readability, fewer long-winded comments. Go for names like
customerTaxValue, cityUnemploymentRate, clientSalutationString, etc.
Drawbacks
More variables are present in your code. But this is counterbalanced by the ease of reading your
code.
How to Refactor
1. Insert a new line before the relevant expression and declare a new variable there. Assign part of
the complex expression to this variable.
2. Replace that part of the expression with the new variable.
3. Repeat the process for all complex parts of the expression.
Reading is boring
23.08.2026, 11:10 Extract Variable
https://sourcemaking.com/refactoring/extract-variable 2/3

RETURN READ NEXT
Inline Temp    Inline Method
Design Patterns AntiPatterns Refactoring
UML
My account Forum Contact us About us
© 2007-2026 SourceMaking.com
All rights reserved.
Terms / Privacy policy
Aren't you bored of reading so much? Try out our new interactive learning
course on refactoring. It has more content and much more fun.
 Learn more
23.08.2026, 11:10 Extract Variable
https://sourcemaking.com/refactoring/extract-variable 3/3
