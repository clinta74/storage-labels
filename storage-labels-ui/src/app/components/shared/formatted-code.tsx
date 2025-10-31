import React from 'react';
import { Box, Typography, useTheme } from '@mui/material';
import { useUser } from '../../providers/user-provider';

interface FormattedCodeProps {
    code: string;
    variant?: 'body1' | 'body2' | 'h6' | 'subtitle1' | 'subtitle2';
}

type ColorType = 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success' | 'default';

interface ParsedSegment {
    text: string;
    color: ColorType;
}

const parseColorPattern = (pattern: string, code: string): ParsedSegment[] => {
    if (!pattern) {
        return [{ text: code, color: 'default' }];
    }

    try {
        const segments: ParsedSegment[] = [];
        const parts = pattern.split(',');
        let currentIndex = 0;

        for (let i = 0; i < parts.length; i++) {
            const part = parts[i].trim();
            
            if (part === '*') {
                // Skip middle characters (no color)
                // Find how many chars are left for remaining patterns
                let remainingChars = 0;
                for (let j = i + 1; j < parts.length; j++) {
                    const nextPart = parts[j].trim();
                    if (nextPart !== '*') {
                        const [lengthStr] = nextPart.split(':');
                        remainingChars += parseInt(lengthStr) || 0;
                    }
                }
                
                const skipLength = code.length - currentIndex - remainingChars;
                if (skipLength > 0) {
                    segments.push({
                        text: code.substring(currentIndex, currentIndex + skipLength),
                        color: 'default'
                    });
                    currentIndex += skipLength;
                }
                continue;
            }

            const [lengthStr, colorStr] = part.split(':');
            const length = parseInt(lengthStr) || 0;
            const color = (colorStr || 'default') as ColorType;

            if (currentIndex + length <= code.length) {
                segments.push({
                    text: code.substring(currentIndex, currentIndex + length),
                    color
                });
                currentIndex += length;
            }
        }

        // Add any remaining characters
        if (currentIndex < code.length) {
            segments.push({
                text: code.substring(currentIndex),
                color: 'default'
            });
        }

        return segments;
    } catch {
        // If parsing fails, return the whole code without formatting
        return [{ text: code, color: 'default' }];
    }
};

export const FormattedCode: React.FC<FormattedCodeProps> = ({ code, variant = 'body1' }) => {
    const { user } = useUser();
    const theme = useTheme();
    
    const pattern = user?.preferences?.codeColorPattern || '';

    // If no pattern set, return plain code
    if (!pattern) {
        return <Typography variant={variant}>{code}</Typography>;
    }

    const segments = parseColorPattern(pattern, code);

    return (
        <Typography variant={variant} component="span">
            {segments.map((segment, idx) => {
                const color = segment.color === 'default' 
                    ? undefined 
                    : theme.palette[segment.color].main;

                return (
                    <Box
                        key={idx}
                        component="span"
                        sx={{
                            color: color,
                            fontWeight: segment.color !== 'default' ? 600 : undefined,
                        }}
                    >
                        {segment.text}
                    </Box>
                );
            })}
        </Typography>
    );
};
