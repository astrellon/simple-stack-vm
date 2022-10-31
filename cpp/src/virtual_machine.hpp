#pragma once

#include <functional>
#include <unordered_map>
#include <string>
#include <memory>

#include "operator.hpp"
#include "code_line.hpp"
#include "scope.hpp"
#include "script.hpp"
#include "function.hpp"
#include "fixed_stack.hpp"
#include "./values/ivalue.hpp"
#include "./values/array_value.hpp"
#include "./values/string_value.hpp"
#include "./values/bool_value.hpp"

namespace stack_vm
{
    class scope_frame
    {
        public:
            // Fields
            int line_counter;
            std::shared_ptr<const function> function;
            std::shared_ptr<scope> scope;

            // Constructor
            scope_frame() : line_counter(0), function(nullptr), scope(nullptr) { }
            scope_frame(int line_counter, std::shared_ptr<const stack_vm::function> function, std::shared_ptr<stack_vm::scope> scope) : line_counter(line_counter), function(function), scope(scope) { }

            // Methods
    };

    class virtual_machine
    {
        public:
            // Fields
            bool running;
            bool paused;
            std::shared_ptr<const scope> builtin_scope;
            std::shared_ptr<const function> current_code;

            // Constructor
            virtual_machine(int stackSize);

            // Methods
            void reset();
            void change_to_script(std::shared_ptr<const script> input);
            void execute(std::shared_ptr<const script> input);
            void step();
            void jump(const std::string &label);

            // Function methods
            std::shared_ptr<const array_value> get_args(int num_args);
            void call_function(const ivalue &value, int num_args, bool push_to_stack_trace);
            bool try_return();
            void call_return();
            void execute_function(std::shared_ptr<const function> func, std::shared_ptr<const array_value> args, bool push_to_stack_trace);
            void push_stack_trace(const scope_frame &frame);

            // Stack methods
            inline std::shared_ptr<const ivalue> pop_stack()
            {
                std::shared_ptr<const ivalue> result;
                if (!stack.pop(result))
                {
                    throw std::runtime_error("Unable to pop stack, empty stack");
                }
                return result;
            }

            inline void push_stack(std::shared_ptr<const ivalue> input)
            {
                if (!stack.push(input))
                {
                    throw std::runtime_error("Unable to push stack, stack full");
                }
            }

            inline std::shared_ptr<const ivalue> peek_stack() const
            {
                std::shared_ptr<const ivalue> result;
                if (!stack.peek(result))
                {
                    throw std::runtime_error("Unable to peek stack, empty stack");
                }
                return result;
            }

            void print_stack_debug();

        private:
            // Fields
            fixed_stack<std::shared_ptr<const ivalue>> stack;
            fixed_stack<scope_frame> stack_trace;
            std::shared_ptr<scope> current_scope;
            std::shared_ptr<scope> global_scope;

            int program_counter;

            // Methods
            inline const ivalue *get_operator_arg(const code_line &input)
            {
                if (input.value)
                {
                    return input.value.get();
                }

                return pop_stack().get();
            }

            template <typename T>
            inline const T *get_operator_arg(const code_line &input)
            {
                auto result = input.value;
                if (!input.value)
                {
                    result = pop_stack();
                }

                return dynamic_cast<const T *>(result.get());
            }
    };
} // stack_vm