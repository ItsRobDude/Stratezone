shader_type spatial;
render_mode cull_back;

instance uniform vec4 team_color : source_color = vec4(0.275, 0.761, 0.8, 1.0);

void fragment() {
    float team_mask = UV.x;
    ALBEDO = mix(COLOR.rgb, team_color.rgb, team_mask);
    ROUGHNESS = 0.78;
    METALLIC = 0.1;
    EMISSION = team_color.rgb * team_mask * 0.65;
}
