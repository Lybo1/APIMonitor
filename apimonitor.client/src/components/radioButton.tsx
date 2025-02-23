import React from 'react';
import { CustomRadioButtonProps } from '../types/CustomRadioButton';

const CustomRadioButton: React.FC<CustomRadioButtonProps> = ({
                                                                 id,
                                                                 name,
                                                                 value,
                                                                 checked,
                                                                 onChange,
                                                                 children,
                                                             }) => {
    return (
        <label htmlFor={id} className="flex items-center space-x-2 cursor-pointer">
            <input
                type="radio"
                id={id}
                name={name}
                value={value}
                checked={checked}
                onChange={onChange}
                className="peer hidden"
            />
            <span className="w-5 h-5 rounded-full border-2 border-white peer-checked:bg-white peer-checked:ring-2 peer-checked:ring-blue-500 peer-checked:ring-offset-2 peer-checked:ring-offset-gray-800"></span>
            <span className="text-white">{children}</span>
        </label>
    );
};

export default CustomRadioButton;
