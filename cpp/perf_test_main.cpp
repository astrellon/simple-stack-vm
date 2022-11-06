#include <iostream>

#include <random>
#include <fstream>
#include <chrono>

#include "src/assembler.hpp"
#include "src/parser.hpp"
#include "src/values/values.hpp"
#include "src/virtual_machine.hpp"

std::random_device _rd;
std::mt19937 _rand(_rd());

int counter = 0;

std::shared_ptr<stack_vm::scope> create_custom_scope()
{
    auto result = std::make_shared<stack_vm::scope>();

    result->define("rand", [](stack_vm::virtual_machine &vm, const stack_vm::array_value &args) -> void
    {
        std::uniform_real_distribution<double> dist(0.0, 1.0);
        vm.push_stack(dist(_rand));
    });

    result->define("add", [](stack_vm::virtual_machine &vm, const stack_vm::array_value &args) -> void
    {
        auto num1 = vm.pop_stack();
        auto num2 = vm.pop_stack();
        vm.push_stack(num1.get_number() + num2.get_number());
    });

    result->define("isDone", [](stack_vm::virtual_machine &vm, const stack_vm::array_value &args) -> void
    {
        counter++;
        vm.push_stack(counter >= 1000000);
    });

    result->define("done", [](stack_vm::virtual_machine &vm, const stack_vm::array_value &args) -> void
    {
        auto total = vm.pop_stack();

        std::cout << "Done: " << total.get_number() << "\n";
    });

    return result;
}

int main()
{
    std::ifstream input_file;
    input_file.open("../../examples/perfTest.lisp");
    if (!input_file)
    {
        std::cout << "Could not find file to open!\n";
        return -1;
    }

    auto custom_scope = create_custom_scope();

    auto parsed = stack_vm::parser::read_from_stream(input_file);
    stack_vm::assembler assembler;
    assembler.builtin_scope.combine_scope(*custom_scope);

    auto script = assembler.parse_from_value(parsed);

    stack_vm::virtual_machine vm(16);

    auto start = std::chrono::steady_clock::now();
    vm.execute(script);
    auto end = std::chrono::steady_clock::now();

    std::cout << "Time taken: " << std::chrono::duration_cast<std::chrono::milliseconds>(end - start).count() << "ms\n";

    return 0;
}