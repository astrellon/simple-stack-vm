#include "assembler.hpp"

#include <iostream>
#include <sstream>
#include <unordered_map>

#include "./parser.hpp"
#include "../utils.hpp"
#include "../values/function_value.hpp"
#include "../values/variable_value.hpp"
#include "../values/value_property_access.hpp"
#include "../standard_library/standard_math_library.hpp"
#include "../virtual_machine.hpp"

namespace lysithea_vm
{
    const std::string assembler::keyword_function("function");
    const std::string assembler::keyword_loop("loop");
    const std::string assembler::keyword_continue("continue");
    const std::string assembler::keyword_break("break");
    const std::string assembler::keyword_if("if");
    const std::string assembler::keyword_unless("unless");
    const std::string assembler::keyword_set("set");
    const std::string assembler::keyword_define("define");
    const std::string assembler::keyword_jump("jump");
    const std::string assembler::keyword_return("return");

    assembler::assembler() : label_count(0)
    {

    }

    std::shared_ptr<script> assembler::parse_from_text(const std::string &source_name, const std::string &input)
    {
        std::stringstream stream(input);
        return parse_from_stream(source_name, stream);
    }

    std::shared_ptr<script> assembler::parse_from_stream(const std::string &source_name, std::istream &input)
    {
        this->source_text = parser::split_stream(input);
        this->source_name = source_name;

        auto parsed = parser::read_from_text(*source_text);
        return parse_from_value(parsed);
    }

    std::shared_ptr<script> assembler::parse_from_value(const token &input)
    {
        auto code = parse_global_function(input);

        auto script_scope = std::make_shared<scope>();
        script_scope->combine_scope(builtin_scope);

        return std::make_shared<script>(script_scope, code);
    }

    std::shared_ptr<function> assembler::parse_global_function(const token &input)
    {
        code_line_list temp_code_lines;
        for (const auto &iter : input.list_data)
        {
            auto lines = parse(*iter);
            push_range(temp_code_lines, lines);
        }

        std::vector<std::string> empty_parameters;
        auto code = process_temp_function(empty_parameters, temp_code_lines, "global");

        return code;
    }

    assembler::code_line_list assembler::parse(const token &input)
    {
        code_line_list result;
        // auto array_input = dynamic_cast<const token_list *>(&input);
        if (input.type == token_type::list)
        {
            if (input.list_data.size() == 0)
            {
                return result;
            }

            const auto &first_token = *input.list_data[0];
            // If the first item in an array is a symbol we assume that it is a function call or a label
            // auto first = dynamic_cast<token *>(first_token.get());
            if (first_token.type == token_type::value)
            {
                auto first_symbol_value = first_token.token_value.get_complex<variable_value>();
                if (first_symbol_value)
                {
                    if (first_symbol_value->is_label())
                    {
                        result.emplace_back(first_symbol_value->data);
                        return result;
                    }

                    // Check for keywords
                    auto keyword_parse = parse_keyword(first_symbol_value->data, input);
                    if (keyword_parse.size() > 0)
                    {
                        return keyword_parse;
                    }

                    keyword_parsing_stack.push_back("func-call");

                    // Handle general opcode or function call.
                    for (auto iter = input.list_data.cbegin() + 1; iter != input.list_data.cend(); ++iter)
                    {
                        push_range(result, parse(*iter->get()));
                    }

                    push_range(result, optimise_call_symbol_value(first_token, first_symbol_value->data, input.list_data.size() - 1));

                    keyword_parsing_stack.pop_back();

                    return result;
                }
            }
        }
        else
        {
            // auto input_token = dynamic_cast<const token *>(&input);
            if (input.type == token_type::value)
            {
                auto symbol_value = input.token_value.get_complex<variable_value>();
                if (symbol_value && !symbol_value->is_label())
                {
                    return optimise_get_symbol_value(input, symbol_value->data);
                }
            }
        }

        result.emplace_back(vm_operator::push, input);
        return result;
    }

    assembler::code_line_list assembler::parse_define_set(const token &input, bool is_define)
    {
        auto op_code = is_define ? vm_operator::define : vm_operator::set;
        // Parse the last value as the definable/set-able value.
        auto result = parse(*input.list_data.back());

        // Loop over all the middle inputs as the values to set.
        // Multiple variables can be set when a function returns multiple results.
        for (auto i = input.list_data.size() - 2; i >= 1; i--)
        {
            result.emplace_back(op_code, *input.list_data[i]);
        }
        return result;
    }

    assembler::code_line_list assembler::parse_loop(const token &input)
    {
        if (input.list_data.size() < 3)
        {
            throw std::runtime_error("Loop input has too few inputs");
        }

        auto loop_label_num = label_count++;
        std::stringstream ss_label_start(":LoopStart");
        ss_label_start << loop_label_num;
        auto label_start = std::make_shared<string_value>(ss_label_start.str());

        std::stringstream ss_label_end(":LoopEnd");
        ss_label_end << loop_label_num;
        auto label_end = std::make_shared<string_value>(ss_label_end.str());

        loop_stack.emplace_back(label_start, label_end);

        code_line_list result;
        result.emplace_back(ss_label_start.str());

        const auto &comparison_token = *input.list_data[1];
        if (comparison_token.type != token_type::list)
        {
            throw std::runtime_error("Loop comparison input needs to be an array");
        }

        push_range(result, parse(comparison_token));
        result.emplace_back(vm_operator::jump_false, comparison_token.copy(label_end));

        for (auto i = 2; i < input.list_data.size(); i++)
        {
            push_range(result, parse(*input.list_data[i]));
        }

        result.emplace_back(vm_operator::jump, comparison_token.copy(label_start));
        result.emplace_back(ss_label_end.str());

        loop_stack.pop_back();
        return result;
    }

    assembler::code_line_list assembler::parse_cond(const token &input, bool is_if_statement)
    {
        if (input.list_data.size() < 3)
        {
            throw std::runtime_error("Condition input has too few inputs");
        }
        if (input.list_data.size() < 3)
        {
            throw std::runtime_error("Condition input has too many inputs!");
        }

        auto if_label_num = label_count++;

        std::stringstream ss_label_else(":CondElse");
        ss_label_else << if_label_num;
        auto label_else = std::make_shared<string_value>(ss_label_else.str());

        std::stringstream ss_label_end(":CondEnd");
        ss_label_end << if_label_num;
        auto label_end = std::make_shared<string_value>(ss_label_end.str());

        auto has_else_call = input.list_data.size() == 4;
        auto jump_operator = is_if_statement ? vm_operator::jump_false : vm_operator::jump_true;

        const auto &comparison_token = *input.list_data[1];
        if (comparison_token.type != token_type::list)
        {
            throw std::runtime_error("Condition needs comparison to be an array");
        }

        const auto &first_block_token = *input.list_data[2];
        if (first_block_token.type != token_type::list)
        {
            throw std::runtime_error("Condition needs first block to be an array");
        }

        auto result = parse(comparison_token);

        if (has_else_call)
        {
            // Jump to else if the condition doesn't match
            result.emplace_back(jump_operator, comparison_token.copy(label_else));

            // First block of code
            push_range(result, parse_flatten(first_block_token));
            // Jump after the condition, skipping second block of code.
            result.emplace_back(vm_operator::jump, first_block_token.copy(label_end));

            // Jump target for else
            result.emplace_back(ss_label_else.str());

            // Second 'else' block of code
            const auto &second_block_token = *input.list_data[3];
            if (second_block_token.type != token_type::list)
            {
                throw std::runtime_error("Condition else needs second block to be an array");
            }

            push_range(result, parse_flatten(second_block_token));
        }
        else
        {
            result.emplace_back(jump_operator, comparison_token.copy(label_end));

            push_range(result, parse_flatten(first_block_token));
        }

        result.emplace_back(ss_label_end.str());

        return result;
    }

    assembler::code_line_list assembler::parse_flatten(const token &input)
    {
        if (input.type == token_type::list)
        {
            auto all_array = true;
            for (const auto &iter : input.list_data)
            {
                if (iter->type != token_type::list)
                {
                    all_array = false;
                    break;
                }
            }

            if (all_array)
            {
                code_line_list result;
                for (const auto &iter : input.list_data)
                {
                    push_range(result, parse(*iter));
                }
                return result;
            }
        }

        return parse(input);
    }

    assembler::code_line_list assembler::parse_loop_jump(const token &token, const std::string &keyword, bool jump_to_start)
    {
        if (loop_stack.size() == 0)
        {
            throw std::runtime_error("Unexpected keyword outside of loop");
        }

        auto loop_label = loop_stack.back();

        code_line_list result;
        result.emplace_back(vm_operator::jump, token.copy(jump_to_start ? loop_label.start : loop_label.end));
        return result;
    }

    std::shared_ptr<function> assembler::parse_function(const token &input)
    {
        std::string name;
        auto offset = 0;

        auto name_var_check = input.list_data[1]->token_value.get_complex<const variable_value>();
        if (name_var_check)
        {
            name = name_var_check->data;
            offset = 1;
        }
        else
        {
            auto name_string_check = input.list_data[1]->token_value.get_complex<const string_value>();
            if (name_string_check)
            {
                name = name_string_check->data;
                offset = 1;
            }
        }

        std::vector<std::string> parameters;
        auto parameters_array = input.list_data[1 + offset]->list_data;
        for (auto iter : parameters_array)
        {
            parameters.emplace_back(iter->to_string(0));
        }

        code_line_list temp_code_lines;
        for (auto i = 2 + offset; i < input.list_data.size(); i++)
        {
            push_range(temp_code_lines, parse(*input.list_data[i]));
        }

        return process_temp_function(parameters, temp_code_lines, name);
    }

    // assembler::code_line_list assembler::parse_change_variable(value input, builtin_function_value change_func)
    // {
    //     auto var_name = std::make_shared<string_value>(input.to_string());
    //     value num_args(1);

    //     array_vector call_array_args;
    //     call_array_args.emplace_back(std::make_shared<builtin_function_value>(change_func));
    //     call_array_args.emplace_back(num_args);
    //     auto call_array_values = std::make_shared<array_value>(call_array_args, false);

    //     code_line_list result;
    //     result.emplace_back(vm_operator::get, token(value(var_name)));
    //     result.emplace_back(vm_operator::call_direct, call_array_values);
    //     result.emplace_back(vm_operator::set, value(var_name));

    //     return result;
    // }

    assembler::code_line_list assembler::parse_jump(const token &input)
    {
        auto result = parse(*input.list_data[1]);
        if (result.size() == 1 && result[0].op == vm_operator::push)
        {
            const auto &first_token = result[0].argument;
            if (!first_token.token_value.is_undefined())
            {
                code_line_list new_result;
                new_result.emplace_back(vm_operator::jump, result[0].argument);
                return new_result;
            }
        }
        result.emplace_back(vm_operator::jump, input.to_empty());
        return result;
    }

    assembler::code_line_list assembler::parse_return(const token &input)
    {
        code_line_list result;
        for (auto iter = input.list_data.cbegin() + 1; iter != input.list_data.cend(); ++iter)
        {
            push_range(result, parse(*iter->get()));
        }
        result.emplace_back(vm_operator::call_return, input.to_empty());
        return result;
    }

    assembler::code_line_list assembler::parse_function_keyword(const token &input)
    {
        auto function = parse_function(input);
        auto function_value = std::make_shared<lysithea_vm::function_value>(function);

        code_line_list result;
        result.emplace_back(vm_operator::push, input.copy(value(function_value)));

        auto current_keyword = keyword_parsing_stack.size() > 1 ? keyword_parsing_stack[keyword_parsing_stack.size() - 2] : keyword_function;
        if (function->has_name && current_keyword == keyword_function)
        {
            result.emplace_back(vm_operator::define, input.copy(value(function->name)));
        }
        return result;
    }

    assembler::code_line_list assembler::parse_negative(const token &input)
    {
        if (input.list_data.size() == 3)
        {
            return parse_operator(vm_operator::sub, input);
        }
        else if (input.list_data.size() == 2)
        {
            // If it's a constant already, just push the negative.
            auto first = input.list_data[1]->get_value();
            if (first.is_number())
            {
                code_line_list result;
                result.emplace_back(vm_operator::push, input.list_data[1]->copy(value(-first.get_number())));
                return result;
            }
            else
            {
                auto result = parse(*input.list_data[1]);
                result.emplace_back(vm_operator::unary_negative, input.list_data[1]->to_empty());
                return result;
            }
        }
        else
        {
            throw std::runtime_error("Negative/Sub operator expects 1 or 2 inputs");
        }
    }

    assembler::code_line_list assembler::parse_one_push_input(vm_operator op_code, const token &input)
    {
        if (input.list_data.size() < 2)
        {
            throw std::runtime_error("Operator expects ast least 1 input");
        }

        code_line_list result;
        for (auto iter = input.list_data.cbegin() + 1; iter != input.list_data.cend(); ++iter)
        {
            push_range(result, parse(**iter));
            result.emplace_back(op_code, (*iter)->to_empty());
        }
        return result;
    }

    assembler::code_line_list assembler::parse_operator(vm_operator op_code, const token &input)
    {
        if (input.list_data.size() < 3)
        {
            throw std::runtime_error("Operator expects at least 2 inputs");
        }

        auto result = parse(*input.list_data[1]);
        for (auto iter = input.list_data.cbegin() + 2; iter != input.list_data.cend(); ++iter)
        {
            const auto &token = **iter;
            auto token_value = token.get_value();
            if (token_value.is_number())
            {
                result.emplace_back(op_code, token);
            }
            else
            {
                push_range(result, parse(token));
                result.emplace_back(op_code, input.to_empty());
            }
        }
        return result;
    }

    assembler::code_line_list assembler::parse_one_variable_update(vm_operator op_code, const token &input)
    {
        if (input.list_data.size() < 2)
        {
            throw std::runtime_error("Operator expects at least 1 input");
        }

        code_line_list result;
        for (auto iter = input.list_data.cbegin() + 1; iter != input.list_data.cend(); ++iter)
        {
            auto var_name = (*iter)->get_value().to_string();
            result.emplace_back(op_code, (*iter)->copy(value(var_name)));
        }

        return result;
    }

    assembler::code_line_list assembler::parse_string_concat(const token &input)
    {
        code_line_list result;
        for (auto iter = input.list_data.cbegin() + 1; iter != input.list_data.cend(); ++iter)
        {
            push_range(result, parse(**iter));
        }
        result.emplace_back(vm_operator::string_concat, input.copy(value(input.list_data.size() - 1)));
        return result;
    }

    assembler::code_line_list assembler::transform_assignment_operator(const token &input)
    {
        auto op_code = input.list_data[0]->get_value().to_string();
        op_code = op_code.substr(0, op_code.size() - 1);

        auto var_name = input.list_data[1]->get_value().to_string();
        std::vector<token_ptr> new_code(input.list_data);
        new_code[0] = std::make_shared<token>(input.list_data[0]->copy(value(std::make_shared<variable_value>(op_code))));

        std::vector<token_ptr> wrapped_code;
        wrapped_code.emplace_back(std::make_shared<token>(input.copy(value(std::make_shared<variable_value>("set")))));
        wrapped_code.emplace_back(std::make_shared<token>(input.list_data[1]->copy(value(std::make_shared<variable_value>(var_name)))));
        wrapped_code.emplace_back(std::make_shared<token>(input.location, new_code));

        token wrapped_code_value(input.location, wrapped_code);
        return parse(wrapped_code_value);
    }

    std::vector<temp_code_line> assembler::parse_keyword(const std::string &keyword, const token &input)
    {
        code_line_list result;
        keyword_parsing_stack.push_back(keyword);

        // General Operators
        if (keyword == keyword_function) { result = parse_function_keyword(input); }
        else if (keyword == keyword_continue) { result = parse_loop_jump(input, keyword, true); }
        else if (keyword == keyword_break) { result = parse_loop_jump(input, keyword, false); }
        else if (keyword == keyword_set) { result = parse_define_set(input, false); }
        else if (keyword == keyword_define) { result = parse_define_set(input, true); }
        else if (keyword == keyword_loop) { result = parse_loop(input); }
        else if (keyword == keyword_if) { result = parse_cond(input, true); }
        else if (keyword == keyword_unless) { result = parse_cond(input, false); }
        else if (keyword == keyword_jump) { result = parse_jump(input); }
        else if (keyword == keyword_return) { result = parse_return(input); }

        // Math Operators
        else if (keyword == "+")  { result = parse_operator(vm_operator::add, input); }
        else if (keyword == "-")  { result = parse_negative(input); }
        else if (keyword == "*")  { result = parse_operator(vm_operator::multiply, input); }
        else if (keyword == "/")  { result = parse_operator(vm_operator::divide, input); }
        else if (keyword == "++") { result = parse_one_variable_update(vm_operator::inc, input); }
        else if (keyword == "--") { result = parse_one_variable_update(vm_operator::dec, input); }

        // Comparison Operators
        else if (keyword == "<")  { result = parse_operator(vm_operator::less_than, input); }
        else if (keyword == "<=") { result = parse_operator(vm_operator::less_than_equals, input); }
        else if (keyword == "==") { result = parse_operator(vm_operator::equals, input); }
        else if (keyword == "!=") { result = parse_operator(vm_operator::not_equals, input); }
        else if (keyword == ">")  { result = parse_operator(vm_operator::greater_than, input); }
        else if (keyword == ">=") { result = parse_operator(vm_operator::greater_than_equals, input); }

        // Boolean Operators
        else if (keyword == "&&") { result = parse_operator(vm_operator::op_and, input); }
        else if (keyword == "||") { result = parse_operator(vm_operator::op_or, input); }
        else if (keyword == "!")  { result = parse_one_push_input(vm_operator::op_not, input); }

        // Misc Operators
        else if (keyword == "$")  { result = parse_string_concat(input); }

        // Conjoined Operators
        else if (keyword == "+=" || keyword == "-=" || keyword == "*=" || keyword == "/=" ||
            keyword == "&&=" || keyword == "||=" || keyword == "$=")
        {
            result = transform_assignment_operator(input);
        }

        keyword_parsing_stack.pop_back();

        return result;
    }

    std::vector<temp_code_line> assembler::optimise_call_symbol_value(const token &input, const std::string &variable, int num_args)
    {
        code_line_list result;
        value num_arg_value(num_args);

        std::shared_ptr<string_value> parent_key;
        std::shared_ptr<array_value> property;
        auto is_property = is_get_property_request(variable, parent_key, property);

        // Check if we know about the parent object? (eg: string.length, the parent is the string object)
        value found_parent;
        if (builtin_scope.try_get_key(parent_key->data, found_parent))
        {
            // If the get is for a property? (eg: string.length, length is the property)
            value found_property;
            if (is_property && try_get_property(found_parent, *property, found_property))
            {
                if (found_property.is_function())
                {
                    // If we found the property then we're done and we can just push that known value onto the stack.
                    array_vector call_vector;
                    call_vector.emplace_back(found_property);
                    call_vector.emplace_back(num_arg_value);

                    auto call_value = std::make_shared<array_value>(call_vector, false);
                    result.emplace_back(vm_operator::call_direct, input.copy(call_value));
                    return result;
                }

                throw std::runtime_error("Attempting to call a value that is not a function");
            }
            else if (!is_property)
            {
                // This was not a property request but we found the parent so just push onto the stack.
                if (found_parent.is_function())
                {
                    array_vector call_vector;
                    call_vector.emplace_back(found_parent);
                    call_vector.emplace_back(num_arg_value);

                    auto call_value = std::make_shared<array_value>(call_vector, false);
                    result.emplace_back(vm_operator::call_direct, input.copy(call_value));
                    return result;
                }

                throw std::runtime_error("Attempting to call a value that is not a function");
            }
        }

        // Could not find the parent right now, so look for the parent at runtime.
        result.emplace_back(vm_operator::get, input.copy(parent_key));

        // If this was also a property check also look up the property at runtime.
        if (is_property)
        {
            result.emplace_back(vm_operator::get_property, input.copy(property));
        }
        result.emplace_back(vm_operator::call, input.copy(num_arg_value));

        return result;
    }

    std::vector<temp_code_line> assembler::optimise_get_symbol_value(const token &input, const std::string &variable)
    {
        std::string get_name = variable;
        auto is_argument_unpack = starts_with_unpack(variable);
        if (is_argument_unpack)
        {
            get_name = get_name.substr(3);
        }

        std::shared_ptr<string_value> parent_key;
        std::shared_ptr<array_value> property;
        auto is_property = is_get_property_request(get_name, parent_key, property);

        code_line_list result;

        value found_parent;
        // Check if we know about the parent object? (eg: string.length, the parent is the string object)
        if (builtin_scope.try_get_key(parent_key->data, found_parent))
        {
            // If the get is for a property? (eg: string.length, length is the property)
            if (is_property)
            {
                value found_property;
                if (try_get_property(found_parent, *property, found_property))
                {
                    // If we found the property then we're done and we can just push that known value onto the stack.
                    result.emplace_back(vm_operator::push, input.copy(found_property));
                }
                else
                {
                    // We didn't find the property at compile time, so look it up at run time.
                    result.emplace_back(vm_operator::push, input.copy(found_parent));
                    result.emplace_back(vm_operator::get_property, input.copy(property));
                }
            }
            else
            {
                // This was not a property request but we found the parent so just push onto the stack.
                result.emplace_back(vm_operator::push, input.copy(found_parent));
            }
        }
        else
        {
            // Could not find the parent right now, so look for the parent at runtime.
            result.emplace_back(vm_operator::get, input.copy(parent_key));

            // If this was also a property check also look up the property at runtime.
            if (is_property)
            {
                result.emplace_back(vm_operator::get_property, input.copy(property));
            }
        }

        if (is_argument_unpack)
        {
            result.emplace_back(vm_operator::to_argument, input.to_empty());
        }

        return result;
    }

    bool assembler::is_get_property_request(const std::string &input, std::shared_ptr<string_value> &parent_key, std::shared_ptr<array_value> &property)
    {
        auto find = input.find('.');
        if (find != input.npos)
        {
            auto split = string_split(input, ".");
            parent_key = std::make_shared<string_value>(split[0]);

            array_vector property_vector;
            for (auto i = 1; i < split.size(); i++)
            {
                property_vector.emplace_back(std::make_shared<string_value>(split[i]));
            }
            property = std::make_shared<array_value>(property_vector, false);

            return true;
        }

        parent_key = std::make_shared<string_value>(input);
        return false;
    }

    std::shared_ptr<function> assembler::process_temp_function(const std::vector<std::string> &parameters, const assembler::code_line_list &temp_code_lines, const std::string &name)
    {
        std::unordered_map<std::string, int> labels;
        std::vector<code_line> code;
        std::vector<code_location> locations;

        for (const auto &temp_line : temp_code_lines)
        {
            if (temp_line.is_label())
            {
                labels[temp_line.jump_label] = code.size();
            }
            else
            {
                locations.emplace_back(temp_line.argument.location);
                // TODO: Check if the get_value needs to handle empty
                code.emplace_back(temp_line.op, temp_line.argument.get_value());
            }
        }

        auto symbols = std::make_shared<debug_symbols>(source_name, source_text, locations);

        return std::make_shared<function>(code, parameters, labels, name, symbols);
    }
} // lysithea_vm