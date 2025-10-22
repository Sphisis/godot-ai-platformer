shader_type canvas_item;

// Glitch effect parameters
uniform float glitch_strength : hint_range(0.0, 1.0) = 0.05;
uniform float noise_intensity : hint_range(0.0, 1.0) = 0.1;
uniform float slice_intensity : hint_range(0.0, 50.0) = 1.5;
uniform float slice_speed : hint_range(0.0, 10.0) = 5.0;
uniform float block_size : hint_range(1.0, 50.0) = 15.0;
uniform float block_distortion : hint_range(0.0, 50.0) = 2.0;
uniform float static_scale : hint_range(1.0, 100.0) = 40.0;
uniform vec2 rgb_split = vec2(0.5, -0.5);

// Scanline parameters
uniform float scanline_intensity : hint_range(0.0, 1.0) = 0.3;
uniform float scanline_count : hint_range(50.0, 500.0) = 200.0;
uniform float scanline_speed : hint_range(0.0, 2.0) = 0.5;

// CRT glow parameters
uniform float glow_intensity : hint_range(0.0, 1.0) = 0.4;
uniform float glow_spread : hint_range(0.0, 10.0) = 15.0;

// Random function
float random(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453123);
}

// Noise function with fixed quality
float noise(vec2 uv) {
    const float quality = 10.0;
    vec2 i = floor(uv * quality);
    vec2 f = fract(uv * quality);
    float a = random(i);
    float b = random(i + vec2(1.0, 0.0));
    float c = random(i + vec2(0.0, 1.0));
    float d = random(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

void fragment() {
    vec2 uv = SCREEN_UV;
    vec2 text_uv = UV;
    float time = TIME * 2.0;  // Faster time scale
    
    // Sample original texture for alpha
    vec4 original = texture(TEXTURE, text_uv);
    float alpha = original.a * COLOR.a;  // Multiply by modulate alpha for fade support
    
    if (original.a > 0.01) {
        // Block-based displacement
        vec2 block_uv = floor(text_uv * block_size) / block_size;
        float block_noise = random(block_uv + floor(time * 2.0));
        
        // Sharp horizontal slices
        float slice = floor(uv.y * slice_intensity + time * slice_speed);
        float slice_noise = random(vec2(slice, time));
        
        // Apply jagged displacement
        vec2 displacement = vec2(0.0);
        displacement.x = (block_noise * 2.0 - 1.0) * 0.02 * glitch_strength;
        displacement.x += step(slice_noise, 0.5) * (slice_noise - 0.5) * 0.04;
        
        // Add sudden jumps
        if (random(vec2(floor(time * 4.0))) > 0.8) {  // Less frequent jumps
            displacement.x += (random(vec2(floor(uv.y * block_distortion))) * 2.0 - 1.0) * 0.02;
        }
        
        // Sharp noise pattern
        float sharp_noise = step(0.5, random(text_uv * static_scale + vec2(time * 100.0)));
        
        // Aggressive RGB splitting
        vec2 split = rgb_split * 0.001 * glitch_strength;
        vec4 red = texture(TEXTURE, text_uv + displacement + split);
        vec4 green = texture(TEXTURE, text_uv + displacement);
        vec4 blue = texture(TEXTURE, text_uv + displacement - split);
        
        // Combine with harsh noise
        vec4 final_color = vec4(red.r, green.g, blue.b, alpha);
        
        // Add white noise bursts
        float noise_burst = step(0.98, random(vec2(time * 10.0))) * 
                          step(0.5, random(text_uv * 100.0 + time));
        
        // Blocky color corruption
        if (block_noise > 0.9) {
            final_color.rgb = mix(final_color.rgb, 
                                final_color.gbr, 
                                block_noise);
        }
        
        // Apply noise and static
        final_color.rgb = mix(final_color.rgb, 
                            vec3(sharp_noise), 
                            noise_intensity * 0.2);
        final_color.rgb += vec3(noise_burst) * 0.5;
        
        // Add scanlines
        float scanline = sin((uv.y + TIME * scanline_speed) * scanline_count * 3.14159) * 0.5 + 0.5;
        scanline = pow(scanline, 2.0);  // Make lines sharper
        final_color.rgb *= 1.0 - (scanline * scanline_intensity);
        
        // CRT bloom/glow effect - sample surrounding pixels
        vec2 pixel_size = TEXTURE_PIXEL_SIZE * glow_spread;
        vec3 glow = vec3(0.0);
        float glow_samples = 0.0;
        
        // Sample in a small cross pattern around the current pixel
        for(float x = -1.0; x <= 1.0; x += 1.0) {
            for(float y = -1.0; y <= 1.0; y += 1.0) {
                if (x == 0.0 && y == 0.0) continue;
                vec2 offset = vec2(x, y) * pixel_size;
                vec4 sample_color = texture(TEXTURE, text_uv + displacement + offset);
                glow += sample_color.rgb * sample_color.a;
                glow_samples += 1.0;
            }
        }
        
        // Average and apply glow
        glow /= glow_samples;
        final_color.rgb += glow * glow_intensity;
        
        COLOR = final_color;
    } else {
        COLOR = original;
    }
}