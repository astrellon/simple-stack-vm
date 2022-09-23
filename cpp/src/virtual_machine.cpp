#include "virtual_machine.hpp"

namespace stack_vm
{
    virtual_machine::virtual_machine(int stack_size, stack_vm::run_handler global_run_handler) : stack(stack_size), stack_trace(stack_size), program_counter(0), running(false), paused(false), global_run_handler(global_run_handler)
    {
    }

    void virtual_machine::add_scope(std::shared_ptr<scope> scope)
    {
        scopes[scope->name] = scope;
    }

    void virtual_machine::add_scopes(std::vector<std::shared_ptr<scope>> scopes)
    {
        for (auto &scope : scopes)
        {
            add_scope(scope);
        }
    }

    void virtual_machine::add_run_handler(const std::string &handler_name, stack_vm::run_handler handler)
    {
        run_handlers[handler_name] = handler;
    }

    void virtual_machine::set_current_scope(const std::string &scope_name)
    {
        auto find = scopes.find(scope_name);
        if (find == scopes.end())
        {
            throw std::runtime_error("Unable to find scope");
        }
        else
        {
            current_scope = find->second;
        }
    }

    void virtual_machine::reset()
    {
        program_counter = 0;
        stack.clear();
        stack_trace.clear();
        running = false;
        paused = false;
    }

    value virtual_machine::get_arg(const code_line &input)
    {
        if (input.value.has_value())
        {
            return input.value.value();
        }

        return pop_stack();
    }

    void virtual_machine::step()
    {
        if (program_counter >= current_scope->code.size())
        {
            running = false;
            return;
        }

        // print_stack_debug();

        const auto &code_line = current_scope->code[program_counter++];

        switch (code_line.op)
        {
            default:
            {
                throw std::runtime_error("Unknown operator");
            }
            case vm_operator::push:
            {
                if (!code_line.value.has_value())
                {
                    auto top = peek_stack();
                    stack.push(top);
                }
                else
                {
                    stack.push(code_line.value.value());
                }
                break;
            }
            case vm_operator::swap:
            {
                const auto &value = get_arg(code_line);
                if (value.is_number())
                {
                    swap(static_cast<int>(value.get_number()));
                }
                else
                {
                    throw std::runtime_error("Swap operator needs a number value");
                }
                break;
            }
            case vm_operator::jump:
            {
                const auto &label = get_arg(code_line);
                jump(label);
                break;
            }
            case vm_operator::jump_true:
            {
                const auto &label = get_arg(code_line);
                const auto &top = pop_stack();
                if (top.is_true())
                {
                    jump(label);
                }
                break;
            }
            case vm_operator::jump_false:
            {
                const auto &label = get_arg(code_line);
                const auto &top = pop_stack();
                if (top.is_false())
                {
                    jump(label);
                }
                break;
            }
            case vm_operator::call:
            {
                const auto &label = get_arg(code_line);
                call(label);
                break;
            }
            case vm_operator::call_return:
            {
                call_return();
                break;
            }
            case vm_operator::run:
            {
                const auto &top = get_arg(code_line);
                run_command(top);
                break;
            }
        }
    }

    void virtual_machine::run_command(const value &input)
    {
        if (input.is_array())
        {
            auto arr = input.get_array();
            auto ns = arr->at(0).get_string();
            auto find_ns = run_handlers.find(*ns.get());
            if (find_ns == run_handlers.end())
            {
                throw std::runtime_error("Unable to find namespace for run command");
            }

            find_ns->second(arr->at(1).to_string(), *this);
        }
        else
        {
            global_run_handler(input.to_string(), *this);
        }
    }

    void virtual_machine::call(const value &input)
    {
        stack_trace.push(scope_frame(program_counter, current_scope));
        jump(input);
    }

    void virtual_machine::jump(const value &input)
    {
        if (input.is_string())
        {
            jump(*input.get_string().get());
        }
        else if (input.is_array())
        {
            const auto &list = *std::get<std::shared_ptr<array_value>>(input.data);
            if (list.size() == 0)
            {
                throw std::runtime_error("Cannot jump to empty array");
            }

            const auto &label = list[0].to_string();
            if (list.size() > 1)
            {
                jump(label, list[1].to_string());
            }
            else
            {
                jump(label, "");
            }
        }
    }

    void virtual_machine::jump(const std::string &label)
    {
        if (label.size() > 0)
        {
            if (label[0] == ':')
            {
                jump(label, "");
            }
            else
            {
                jump("", label);
            }
        }
        else
        {
            program_counter = 0;
        }
    }

    void virtual_machine::jump(const std::string &label, const std::string &scope_name)
    {
        if (scope_name.size() > 0)
        {
            auto find = scopes.find(scope_name);
            if (find == scopes.end())
            {
                throw std::runtime_error("Unable to find scope to jump to");
            }
            else
            {
                current_scope = find->second;
            }
        }

        if (label.size() == 0)
        {
            program_counter = 0;
            return;
        }

        auto find_label = current_scope->labels.find(label);
        if (find_label == current_scope->labels.end())
        {
            throw std::runtime_error("Unable to find label in current scope to jump to");
        }

        program_counter = find_label->second;
    }

    void virtual_machine::call_return()
    {
        scope_frame top;
        if (!stack_trace.pop(top))
        {
            throw std::runtime_error("Unable to pop stack track, empty stack");
        }

        current_scope = top.scope;
        program_counter = top.line_counter;
    }

    void virtual_machine::swap(int top_offset)
    {
        stack.swap(top_offset);
    }

    void virtual_machine::print_stack_debug()
    {
        const auto &data = stack.stack_data();
        std::cout << "Stack size: " << data.size() << "\n";
        for (const auto &iter : data)
        {
            std::cout << "- " << iter.to_string() << "\n";
        }
    }
} // namespace stack_vm
