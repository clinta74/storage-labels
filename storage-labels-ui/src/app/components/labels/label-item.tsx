import React from 'react';
import { Box, Typography, useTheme } from '@mui/material';
import { QRCodeSVG } from 'qrcode.react';

interface LabelItemProps {
    code: string;
    codeColorPattern: string;
}

type ColorType = 'primary' | 'secondary' | 'error' | 'warning' | 'info' | 'success' | 'default';

interface ParsedSegment {
    text: string;
    color: ColorType;
}

const parseColorPattern = (pattern: string, code: string): ParsedSegment[] => {
    if (!pattern) return [{ text: code, color: 'default' }];
    try {
        const segments: ParsedSegment[] = [];
        const parts = pattern.split(',');
        let currentIndex = 0;
        for (let i = 0; i < parts.length; i++) {
            const part = parts[i].trim();
            if (part === '*') {
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
                    segments.push({ text: code.substring(currentIndex, currentIndex + skipLength), color: 'default' });
                    currentIndex += skipLength;
                }
                continue;
            }
            const [lengthStr, colorStr] = part.split(':');
            const length = parseInt(lengthStr) || 0;
            const color = (colorStr || 'default') as ColorType;
            if (currentIndex + length <= code.length) {
                segments.push({ text: code.substring(currentIndex, currentIndex + length), color });
                currentIndex += length;
            }
        }
        if (currentIndex < code.length) {
            segments.push({ text: code.substring(currentIndex), color: 'default' });
        }
        return segments;
    } catch {
        return [{ text: code, color: 'default' }];
    }
};

export const LabelItem: React.FC<LabelItemProps> = ({ code, codeColorPattern }) => {
    const theme = useTheme();
    const segments = parseColorPattern(codeColorPattern, code);

    return (
        <Box
            sx={{
                width: '2in',
                height: '2in',
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                justifyContent: 'center',
                gap: '4px',
                border: '1px dashed #ccc',
                boxSizing: 'border-box',
                padding: '4px',
                '@media print': {
                    border: 'none',
                    padding: 0,
                },
            }}
        >
            <QRCodeSVG
                value={code}
                size={120}
                marginSize={0}
                level="M"
            />
            <Typography
                component="span"
                sx={{
                    fontFamily: 'monospace',
                    fontSize: '9pt',
                    fontWeight: 600,
                    letterSpacing: '0.05em',
                    textAlign: 'center',
                    lineHeight: 1,
                }}
            >
                {segments.map((segment, idx) => (
                    <Box
                        key={idx}
                        component="span"
                        sx={{
                            color: segment.color === 'default'
                                ? undefined
                                : theme.palette[segment.color].main,
                            fontWeight: segment.color !== 'default' ? 700 : 600,
                        }}
                    >
                        {segment.text}
                    </Box>
                ))}
            </Typography>
        </Box>
    );
};
