# 🚀 **Marshal Documentation**  

Welcome to the official documentation for **Marshal**! 🎉 Marshal is a compiled programming language built with C# and .NET 8.0, leveraging the LLVM backend to generate machine code. Marshal is an educational project but I'm planning to push it to it's limits! 💻✨

---

## 🛠️ **Getting Started**

Marshal is easy to set up and use. Whether you're an experienced developer or a student eager to learn compiler design, this documentation will help you get started!

### Prerequisites
- ✅ A modern operating system (Linux, macOS, or Windows)
- ✅ .NET SDK 8.0 or newer installed
- ✅ LLVM version 17.0.6 or newer installed

---

## 📦 **Installation**

### Step 1: Clone the Repository
Start by downloading the Marshal project:
```bash
git clone https://github.com/Gray-SS/Marshal.git
```

### Step 2: Build the compiler
Navigate to the project directory and build the compiler:
```bash
cd Marshal/src/Marshal.Compiler
dotnet build
```

## 🌟 Hello World

Here’s your first program in Marshal. 🎉  
Create a file named `hello.msl` and add the following code:

```go
func extern puts(str: string): int;

func main(): int {
    puts("Hello World!");
    return 0;
}
```

As you can probably see, Marshal doesn't have any standard library done yet, so we're using the good old puts function from the c standard library

To compile and run the program:

```bash
marshalc -i hello.ms -o hello
./hello
```

Expected output:

```bash
Hello World!
```

---

## 🔑 Key Features

- **LLVM-Optimized**: Marshal leverages LLVM for machine code generation. (no optimisation layer at the moment) 🚀  
- **Cross-Platform:** Marshal runs on Windows, macOS, and Linux. (probably) 🌐  
- **Extensible:** A great base for adding new features and testing language ideas. 🔧

---

## 📝 Language Syntax
### 📊 Variables

Declare and initialize variables in Marshal:

```go
var age: int = 25;
var name: string = "Alice";
```

### 🧮 Functions

Define reusable functions:

```go
//There is no string concatenation at the moment. Yeah, I really need to implement that
func greet(name: string): void {
    puts("Hello, ");
    puts(name);
    puts("!");
}
```

### 🔄 Control Flow

Control your program's logic with conditions:

```go
//Control the flow of the program with conditions
if (x > 10) {
    puts("Greater than 10!");
} else if (x < 10) {
    puts("10 or less.");
} else {
    puts("10");
}

//while loop running until 'i' is greater than 10
var i: int = 0;
while (i < 10) {
    puts(i);
    i++;
}
```

### Structs
Structures are value types and are allocated on the stack. I took inspiration from c# where I found this variation very interesting. Their value are copied when passing to a function.

```go
struct Vector2 {
    x: int;
    y: int;
}
```

### 🌌 Comments

Add single-line comments with //:

```go
// This is a comment
var x: int = 42; // Another comment

/*
    This is also a comment
*/

```

---

## 🤝 Contributing

Marshal is an open-source project, and we welcome contributions! 🎉 Here's how you can get involved:

- Fork the repository on GitHub.
- Create a new branch for your feature or fix.
- Commit your changes and submit a pull request.

**For major changes, please open an issue first to discuss your ideas.**

## ❓ FAQs
**Q: What platforms does Marshal support?**  
**A:** Marshal supports Windows, macOS, and Linux. You can generate binaries for all these platforms.

**Q: Is Marshal production-ready?**  
**A:** Completely not, as you may have seen it's lacking major features like string concatenation, multiple files compilation, some issues with string because of my shit logic

**Q: Can I extend Marshal with custom features?**  
**A:** Absolutely! Marshal is designed to be extensible. Feel free to add new syntax or functionality and share your improvements.

---

## Thank you for exploring Marshal! ❤️
Let’s push this project to its limits together. Happy coding! 🚀✨